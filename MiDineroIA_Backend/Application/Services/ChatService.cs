using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Interfaces;
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
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatRepository chatRepository,
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IBudgetRepository budgetRepository,
        IClaudeService claudeService,
        ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _budgetRepository = budgetRepository;
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<ChatResponseDto> ProcessMessageAsync(int userId, string message, string? imageBase64)
    {
        try
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

            // 3. Llamar a Claude (por ahora ignoramos imageBase64)
            var claudeResponse = await _claudeService.ProcessUserMessageAsync(message, categoriesJson, currentDate);

            // 4. Marcar mensaje como procesado
            savedUserMessage.AiProcessed = true;

            // 5. Procesar según intent
            ChatResponseDto response;
            switch (claudeResponse.Intent)
            {
                case IntentTypes.REGISTER_TRANSACTION:
                    response = await HandleRegisterTransaction(userId, savedUserMessage.Id, claudeResponse);
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
            var aiMessage = new ChatMessage
            {
                UserId = userId,
                MessageType = MessageTypes.AI_RESPONSE,
                Content = claudeResponse.Message,
                AiProcessed = true
            };
            await _chatRepository.SaveMessageAsync(aiMessage);

            return response;
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

    private async Task<ChatResponseDto> HandleRegisterTransaction(int userId, int chatMessageId, ClaudeResponseDto claudeResponse)
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
            Source = SourceTypes.TEXT,
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

        // Para otros tipos de query, devolver la respuesta de Claude
        return new ChatResponseDto
        {
            Intent = IntentTypes.GENERAL_QUERY,
            Message = claudeResponse.Message,
            Data = new QueryDataDto(queryType),
            NeedsConfirmation = false
        };
    }

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
