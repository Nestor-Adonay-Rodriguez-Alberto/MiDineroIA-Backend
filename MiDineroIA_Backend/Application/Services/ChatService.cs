using System.Text.Json;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Interfaces;
using MiDineroIA_Backend.CrossCutting.Helpers;
using MiDineroIA_Backend.Domain.Constants;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Application.Services;

/// <summary>
/// Servicio orquestador del chat con IA.
/// Coordina el flujo: guardar mensaje → llamar Claude → ejecutar acción → guardar respuesta.
/// </summary>
public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IClaudeService _claudeService;
    private readonly IBlobService _blobService;
    private readonly IOcrService _ocrService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatRepository chatRepository,
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IBudgetRepository budgetRepository,
        IReceiptRepository receiptRepository,
        IClaudeService claudeService,
        IBlobService blobService,
        IOcrService ocrService,
        ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _budgetRepository = budgetRepository;
        _receiptRepository = receiptRepository;
        _claudeService = claudeService;
        _blobService = blobService;
        _ocrService = ocrService;
        _logger = logger;
    }



    // #1: ORQUESTADOR PRINCIPAL PARA PROCESAR MENSAJES DEL CHAT:
    public async Task<ChatResponseDto> ProcessMessageAsync(int userId, string message, string? imageBase64)
    {
        try
        {
            // Si hay imagen, usar flujo de OCR
            if (!string.IsNullOrWhiteSpace(imageBase64))
            {
                return await ProcessImageMessageAsync(userId, message, imageBase64);
            }

            // Flujo normal de texto
            return await ProcessTextMessageAsync(userId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for user {UserId}", userId);

            return new ChatResponseDto
            {
                Intent = IntentTypes.GENERAL_QUERY,
                Message = "Lo siento, no pude procesar tu mensaje. Por favor, intenta de nuevo.",
                NeedsConfirmation = false,
                Data = new QueryDataDto("UNCLEAR")
            };
        }
    }


    // #2: FLUJO COMPLETO PARA MENSAJES DE TEXTO: GUARDAR → CLAUDE → EJECUTAR ACCIÓN SEGÚN INTENT → GUARDAR RESPUESTA
    private async Task<ChatResponseDto> ProcessTextMessageAsync(int userId, string message)
    {
        // 1. Obtener fecha actual y categorías
        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var categoriesJson = await _categoryRepository.GetCategoriesAsJsonAsync(userId);

        // 2. Guardar mensaje del usuario
        var userMessage = new ChatMessage
        {
            UserId = userId,
            MessageType = MessageTypes.USER_TEXT,
            Content = message,
            AiProcessed = false
        };
        var savedUserMessage = await _chatRepository.SaveMessageAsync(userMessage);

        // 3. Llamar a Claude
        var claudeResponse = await _claudeService.ProcessUserMessageAsync(message, categoriesJson, currentDate);

        // 4. Marcar mensaje como procesado
        savedUserMessage.AiProcessed = true;

        // 5. Procesar según intent
        ChatResponseDto response;
        switch (claudeResponse.Intent)
        {
            case IntentTypes.REGISTER_TRANSACTION:
                response = await HandleRegisterTransaction(userId, savedUserMessage.Id, claudeResponse, SourceTypes.TEXT);
                break;

            case IntentTypes.SET_BUDGET:
                response = await HandleSetBudget(userId, claudeResponse);
                break;

            case IntentTypes.GENERAL_QUERY:
            default:
                response = await HandleGeneralQueryAsync(userId, claudeResponse);
                break;
        }

        // 6. Guardar respuesta de IA
        await SaveAiResponse(userId, claudeResponse.Message);

        return response;
    }


    // #3: FLUJO COMPLETO PARA MENSAJES CON IMAGEN: VALIDAR → SUBIR A BLOB → OCR → CLAUDE → GUARDAR TRANSACCIÓN Y RECIBO
    private async Task<ChatResponseDto> ProcessImageMessageAsync(int userId, string message, string imageBase64)
    {
        string? blobUrl = null;
        ChatMessage? savedUserMessage = null;

        try
        {
            // 1. Validar y decodificar imagen
            var (imageBytes, extension) = ImageHelper.ValidateAndDecodeBase64(imageBase64);
            var fileName = ImageHelper.GenerateFileName(extension);

            // 2. Subir imagen a Blob Storage
            try
            {
                blobUrl = await _blobService.UploadImageAsync(userId, imageBytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to blob for user {UserId}", userId);
                return CreateImageErrorResponse("No pude procesar la imagen en este momento. Intenta de nuevo o escríbeme el gasto directamente.");
            }

            // 3. Guardar mensaje del usuario (USER_IMAGE)
            savedUserMessage = new ChatMessage
            {
                UserId = userId,
                MessageType = MessageTypes.USER_IMAGE,
                Content = message,
                ImageUrl = blobUrl,
                AiProcessed = false
            };
            savedUserMessage = await _chatRepository.SaveMessageAsync(savedUserMessage);

            // 4. Obtener URL con SAS token para OCR
            var sasUrl = await _blobService.GetSasUrlAsync(blobUrl);

            // 5. Extraer texto con OCR
            string rawOcrText;
            try
            {
                rawOcrText = await _ocrService.ExtractTextAsync(sasUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR failed for user {UserId}", userId);
                await SaveAiResponse(userId, "No pude leer la factura. Intenta con mejor luz o escríbeme el gasto directamente.");
                return CreateImageErrorResponse("No pude leer la factura. Intenta con mejor luz o escríbeme el gasto directamente.");
            }

            // 6. Verificar que hay suficiente texto
            if (string.IsNullOrWhiteSpace(rawOcrText) || rawOcrText.Length < 10)
            {
                await SaveAiResponse(userId, "No pude leer la factura. Intenta con mejor luz o escríbeme el gasto directamente.");
                return CreateImageErrorResponse("No pude leer la factura. Intenta con mejor luz o escríbeme el gasto directamente.");
            }

            // 7. Procesar con Claude (OCR prompt)
            var currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var categoriesJson = await _categoryRepository.GetCategoriesAsJsonAsync(userId);
            
            ClaudeResponseDto claudeResponse;
            try
            {
                claudeResponse = await _claudeService.ProcessReceiptOcrAsync(rawOcrText, categoriesJson, currentDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Claude OCR processing failed for user {UserId}, retrying...", userId);
                
                // Reintentar una vez
                try
                {
                    claudeResponse = await _claudeService.ProcessReceiptOcrAsync(rawOcrText, categoriesJson, currentDate);
                }
                catch
                {
                    await SaveAiResponse(userId, "No pude procesar los datos de la factura. Intenta de nuevo o escríbeme el gasto directamente.");
                    return CreateImageErrorResponse("No pude procesar los datos de la factura. Intenta de nuevo o escríbeme el gasto directamente.");
                }
            }

            // 8. Marcar mensaje como procesado
            savedUserMessage.AiProcessed = true;

            // 9. Crear transacción con Source = IMAGE
            var transactionData = claudeResponse.GetTransactionData();
            if (transactionData == null)
            {
                await SaveAiResponse(userId, "No pude extraer los datos de la factura. Intenta con otra imagen o escríbeme el gasto.");
                return CreateImageErrorResponse("No pude extraer los datos de la factura. Intenta con otra imagen o escríbeme el gasto.");
            }

            if (!DateTime.TryParse(transactionData.TransactionDate, out var transactionDate))
            {
                transactionDate = DateTime.UtcNow.Date;
            }

            var transaction = new Transaction
            {
                UserId = userId,
                CategoryId = transactionData.CategoryId,
                ChatMessageId = savedUserMessage.Id,
                Amount = transactionData.Amount,
                Description = transactionData.Description,
                Merchant = transactionData.Merchant,
                TransactionDate = transactionDate,
                Source = SourceTypes.IMAGE,
                IsConfirmed = false
            };

            var savedTransaction = await _transactionRepository.CreateAsync(transaction);

            // 10. Crear Receipt
            var receipt = new Receipt
            {
                TransactionId = savedTransaction.Id,
                ImageUrl = blobUrl,
                RawOcrText = rawOcrText,
                AiExtractedJson = JsonSerializer.Serialize(transactionData),
                ConfidenceScore = transactionData.ConfidenceScore
            };

            await _receiptRepository.CreateAsync(receipt);

            // 11. Consultar presupuesto si existe
            BudgetInfoDto? budgetInfo = null;
            var budgetStatus = await _budgetRepository.GetBudgetStatusAsync(
                userId,
                transactionData.CategoryId,
                transactionDate.Year,
                transactionDate.Month);

            if (budgetStatus.Budget > 0)
            {
                budgetInfo = new BudgetInfoDto(
                    budgetStatus.Budget,
                    budgetStatus.Spent + transactionData.Amount,
                    budgetStatus.Remaining - transactionData.Amount
                );
            }

            // 12. Guardar respuesta de IA
            await SaveAiResponse(userId, claudeResponse.Message);

            // 13. Devolver respuesta
            var responseData = new TransactionDataDto
            {
                TransactionType = transactionData.TransactionType,
                Amount = transactionData.Amount,
                CategoryName = transactionData.CategoryName,
                CategoryId = transactionData.CategoryId,
                Description = transactionData.Description,
                Merchant = transactionData.Merchant,
                TransactionDate = transactionData.TransactionDate
            };

            return new ChatResponseDto
            {
                Intent = IntentTypes.REGISTER_TRANSACTION,
                TransactionId = savedTransaction.Id,
                Message = claudeResponse.Message,
                Data = responseData,
                NeedsConfirmation = claudeResponse.NeedsConfirmation,
                BudgetInfo = budgetInfo
            };
        }
        catch (ArgumentException ex)
        {
            // Error de validación de imagen
            _logger.LogWarning(ex, "Image validation failed for user {UserId}", userId);
            return CreateImageErrorResponse(ex.Message);
        }
    }


    // #4: MÉTODO AUXILIAR PARA GUARDAR RESPUESTA DE LA IA EN LA BASE DE DATOS
    private async Task SaveAiResponse(int userId, string message)
    {
        var aiMessage = new ChatMessage
        {
            UserId = userId,
            MessageType = MessageTypes.AI_RESPONSE,
            Content = message,
            AiProcessed = true
        };
        await _chatRepository.SaveMessageAsync(aiMessage);
    }



    // MÉTODO AUXILIAR PARA CREAR RESPUESTA DE ERROR EN FLUJO DE IMAGEN
    private static ChatResponseDto CreateImageErrorResponse(string message)
    {
        return new ChatResponseDto
        {
            Intent = IntentTypes.GENERAL_QUERY,
            Message = message,
            Data = new QueryDataDto("IMAGE_ERROR"),
            NeedsConfirmation = false
        };
    }


    // MÉTODO PARA OBTENER HISTÓRICO DE CHAT
    public async Task<List<ChatMessageDto>> GetHistoryAsync(int userId, int page, int pageSize)
    {
        var messages = await _chatRepository.GetHistoryAsync(userId, page, pageSize);
        
        return messages.Select(m => new ChatMessageDto
        {
            Id = m.Id,
            MessageType = m.MessageType,
            Content = m.Content,
            ImageUrl = m.ImageUrl,
            CreatedAt = m.CreatedAt
        }).ToList();
    }


    // MÉTODO AUXILIAR PARA PROCESAR REGISTRO DE TRANSACCIÓN SEGÚN RESPUESTA DE CLAUDE
    private async Task<ChatResponseDto> HandleRegisterTransaction(int userId, int chatMessageId, ClaudeResponseDto claudeResponse, string source)
    {
        var transactionData = claudeResponse.GetTransactionData();
        if (transactionData == null)
        {
            return CreateErrorResponse("No pude extraer los datos de la transacción. Por favor, reformula tu mensaje.");
        }

        // Parsear fecha de transacción
        if (!DateTime.TryParse(transactionData.TransactionDate, out var transactionDate))
        {
            transactionDate = DateTime.UtcNow.Date;
        }

        // Crear transacción con IsConfirmed = false
        var transaction = new Transaction
        {
            UserId = userId,
            CategoryId = transactionData.CategoryId,
            ChatMessageId = chatMessageId,
            Amount = transactionData.Amount,
            Description = transactionData.Description,
            Merchant = transactionData.Merchant,
            TransactionDate = transactionDate,
            Source = source,
            IsConfirmed = false
        };

        var savedTransaction = await _transactionRepository.CreateAsync(transaction);

        // Consultar presupuesto si existe
        BudgetInfoDto? budgetInfo = null;
        var budgetStatus = await _budgetRepository.GetBudgetStatusAsync(
            userId, 
            transactionData.CategoryId, 
            transactionDate.Year, 
            transactionDate.Month);

        if (budgetStatus.Budget > 0)
        {
            budgetInfo = new BudgetInfoDto(
                budgetStatus.Budget,
                budgetStatus.Spent + transactionData.Amount, // Incluir esta transacción
                budgetStatus.Remaining - transactionData.Amount
            );
        }

        var responseData = new TransactionDataDto
        {
            TransactionType = transactionData.TransactionType,
            Amount = transactionData.Amount,
            CategoryName = transactionData.CategoryName,
            CategoryId = transactionData.CategoryId,
            Description = transactionData.Description,
            Merchant = transactionData.Merchant,
            TransactionDate = transactionData.TransactionDate
        };

        return new ChatResponseDto
        {
            Intent = IntentTypes.REGISTER_TRANSACTION,
            TransactionId = savedTransaction.Id,
            Message = claudeResponse.Message,
            Data = responseData,
            NeedsConfirmation = claudeResponse.NeedsConfirmation,
            BudgetInfo = budgetInfo
        };
    }


    // MÉTODO AUXILIAR PARA PROCESAR CONFIGURACIÓN DE PRESUPUESTO SEGÚN RESPUESTA DE CLAUDE
    private async Task<ChatResponseDto> HandleSetBudget(int userId, ClaudeResponseDto claudeResponse)
    {
        var budgetData = claudeResponse.GetBudgetData();
        if (budgetData == null || budgetData.Budgets.Count == 0)
        {
            return CreateErrorResponse("No pude extraer los datos del presupuesto. Por favor, reformula tu mensaje.");
        }

        var now = DateTime.UtcNow;
        var savedBudgets = new List<BudgetItemDto>();

        foreach (var budget in budgetData.Budgets)
        {
            var monthlyBudget = new MonthlyBudget
            {
                UserId = userId,
                CategoryId = budget.CategoryId,
                Year = budget.Year > 0 ? budget.Year : now.Year,
                Month = budget.Month > 0 ? budget.Month : now.Month,
                Amount = budget.Amount
            };

            await _budgetRepository.UpsertAsync(monthlyBudget);

            savedBudgets.Add(new BudgetItemDto(
                budget.CategoryName,
                budget.CategoryId,
                budget.Amount,
                monthlyBudget.Year,
                monthlyBudget.Month
            ));
        }

        return new ChatResponseDto
        {
            Intent = IntentTypes.SET_BUDGET,
            Message = claudeResponse.Message,
            Data = new BudgetDataDto(savedBudgets),
            NeedsConfirmation = claudeResponse.NeedsConfirmation
        };
    }


    // MÉTODO AUXILIAR PARA PROCESAR CONSULTAS GENERALES SEGÚN RESPUESTA DE CLAUDE (EJ: RESUMEN MENSUAL, ESTADO DE PRESUPUESTO, DETALLE DE CATEGORÍA)
    private async Task<ChatResponseDto> HandleGeneralQueryAsync(int userId, ClaudeResponseDto claudeResponse)
    {
        var queryData = claudeResponse.GetQueryData();
        var queryType = queryData?.QueryType ?? "UNKNOWN";

        // Si es MONTHLY_SUMMARY, consultar datos reales
        if (queryType == "MONTHLY_SUMMARY")
        {
            var now = DateTime.UtcNow;
            var (totalIncome, totalExpenses) = await _transactionRepository.GetMonthlySummaryAsync(
                userId, now.Year, now.Month);
            
            var balance = totalIncome - totalExpenses;
            var monthName = GetSpanishMonthName(now.Month);

            string message;
            if (totalIncome == 0 && totalExpenses == 0)
            {
                message = $"No tienes transacciones registradas en {monthName} todavía.";
            }
            else
            {
                message = $"📊 Tu resumen de {monthName}:\n" +
                         $"• Ingresos: ${totalIncome:N2}\n" +
                         $"• Gastos: ${totalExpenses:N2}\n" +
                         $"• Balance: ${balance:N2}";
                
                if (balance < 0)
                {
                    message += "\n\n⚠️ ¡Cuidado! Tus gastos superan tus ingresos este mes.";
                }
            }

            return new ChatResponseDto
            {
                Intent = IntentTypes.GENERAL_QUERY,
                Message = message,
                Data = new MonthlySummaryDataDto
                {
                    Year = now.Year,
                    Month = now.Month,
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    Balance = balance
                },
                NeedsConfirmation = false
            };
        }

        // Si es BUDGET_STATUS, consultar datos reales del presupuesto
        if (queryType == "BUDGET_STATUS" && queryData?.CategoryId > 0)
        {
            var now = DateTime.UtcNow;
            var budgetStatus = await _budgetRepository.GetBudgetStatusAsync(
                userId, queryData.CategoryId.Value, now.Year, now.Month);

            var categoryName = queryData.CategoryName ?? "la categoría";
            var monthName = GetSpanishMonthName(now.Month);

            string message;
            if (budgetStatus.Budget == 0)
            {
                if (budgetStatus.Spent > 0)
                {
                    message = $"No tienes presupuesto definido para {categoryName} en {monthName}, pero llevas gastado ${budgetStatus.Spent:N2}.";
                }
                else
                {
                    message = $"No tienes presupuesto definido para {categoryName} en {monthName} y no tienes gastos registrados.";
                }
            }
            else
            {
                message = $"📊 Presupuesto de {categoryName} en {monthName}:\n" +
                         $"• Presupuesto: ${budgetStatus.Budget:N2}\n" +
                         $"• Gastado: ${budgetStatus.Spent:N2}\n" +
                         $"• Restante: ${budgetStatus.Remaining:N2}";

                if (budgetStatus.Remaining < 0)
                {
                    message += $"\n\n⚠️ ¡Cuidado! Te has excedido por ${Math.Abs(budgetStatus.Remaining):N2}.";
                }
                else if (budgetStatus.Remaining < budgetStatus.Budget * 0.2m)
                {
                    message += "\n\n⚠️ Te queda menos del 20% de tu presupuesto.";
                }
            }

            return new ChatResponseDto
            {
                Intent = IntentTypes.GENERAL_QUERY,
                Message = message,
                Data = new QueryDataDto(queryType),
                NeedsConfirmation = false,
                BudgetInfo = budgetStatus.Budget > 0
                    ? new BudgetInfoDto(budgetStatus.Budget, budgetStatus.Spent, budgetStatus.Remaining)
                    : null
            };
        }

        // Si es CATEGORY_DETAIL, consultar gasto real de la categoría
        if (queryType == "CATEGORY_DETAIL" && queryData?.CategoryId > 0)
        {
            var now = DateTime.UtcNow;
            var budgetStatus = await _budgetRepository.GetBudgetStatusAsync(
                userId, queryData.CategoryId.Value, now.Year, now.Month);

            var categoryName = queryData.CategoryName ?? "la categoría";
            var monthName = GetSpanishMonthName(now.Month);

            string message;
            if (budgetStatus.Spent == 0)
            {
                message = $"No tienes gastos registrados en {categoryName} durante {monthName}.";
            }
            else
            {
                message = $"📊 Detalle de {categoryName} en {monthName}:\n" +
                         $"• Gastado: ${budgetStatus.Spent:N2}";

                if (budgetStatus.Budget > 0)
                {
                    message += $"\n• Presupuesto: ${budgetStatus.Budget:N2}" +
                              $"\n• Restante: ${budgetStatus.Remaining:N2}";
                }
            }

            return new ChatResponseDto
            {
                Intent = IntentTypes.GENERAL_QUERY,
                Message = message,
                Data = new QueryDataDto(queryType),
                NeedsConfirmation = false,
                BudgetInfo = budgetStatus.Budget > 0
                    ? new BudgetInfoDto(budgetStatus.Budget, budgetStatus.Spent, budgetStatus.Remaining)
                    : null
            };
        }

        // Para otros tipos de query, devolver la respuesta de Claude
        return new ChatResponseDto
        {
            Intent = IntentTypes.GENERAL_QUERY,
            Message = claudeResponse.Message,
            Data = new QueryDataDto(queryType),
            NeedsConfirmation = false
        };
    }


    // MÉTODO AUXILIAR PARA OBTENER NOMBRE DEL MES EN ESPAÑOL
    private static string GetSpanishMonthName(int month)
    {
        return month switch
        {
            1 => "enero",
            2 => "febrero",
            3 => "marzo",
            4 => "abril",
            5 => "mayo",
            6 => "junio",
            7 => "julio",
            8 => "agosto",
            9 => "septiembre",
            10 => "octubre",
            11 => "noviembre",
            12 => "diciembre",
            _ => "este mes"
        };
    }


    // MÉTODO AUXILIAR PARA CREAR RESPUESTA DE ERROR GENERAL
    private static ChatResponseDto CreateErrorResponse(string message)
    {
        return new ChatResponseDto
        {
            Intent = IntentTypes.GENERAL_QUERY,
            Message = message,
            Data = new QueryDataDto("UNCLEAR"),
            NeedsConfirmation = false
        };
    }

}
