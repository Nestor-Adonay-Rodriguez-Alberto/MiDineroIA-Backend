using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.CrossCutting.Auth;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Functions;

public class CategoryFn
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<CategoryFn> _logger;

    public CategoryFn(ICategoryRepository categoryRepository, ITokenValidator tokenValidator, ILogger<CategoryFn> logger)
    {
        _categoryRepository = categoryRepository;
        _tokenValidator = tokenValidator;
        _logger = logger;
    }


    [Function("GetCategories")]
    public async Task<IActionResult> GetCategories([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories")] HttpRequest req)
    {
        try
        {
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId is null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            var categories = await _categoryRepository.GetByUserAsync(userId.Value);

            var groups = categories
                .GroupBy(c => c.CategoryGroup)
                .OrderBy(g => g.Key.DisplayOrder)
                .Select(g => new CategoryGroupDto
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    TransactionType = g.Key.TransactionType,
                    Categories = g.Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name
                    }).ToList()
                })
                .ToList();

            return new OkObjectResult(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categorías");
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }

    [Function("CreateCategory")]
    public async Task<IActionResult> CreateCategory([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "categories")] HttpRequest req)
    {
        try
        {
            var userId = AuthHelper.ExtractUserIdFromRequest(req, _tokenValidator);
            if (userId is null)
            {
                return new UnauthorizedObjectResult(new { error = "Token inválido o expirado" });
            }

            var request = await req.ReadFromJsonAsync<CreateCategoryRequestDto>();
            if (request is null)
            {
                return new BadRequestObjectResult(new { error = "Request body inválido" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new BadRequestObjectResult(new { error = "El nombre de la categoría es requerido" });
            }

            var category = new Category
            {
                CategoryGroupId = request.CategoryGroupId,
                UserId = userId.Value,
                Name = request.Name.Trim(),
                IsDefault = false,
                IsActive = true
            };

            var saved = await _categoryRepository.CreateAsync(category);

            return new OkObjectResult(new CategoryDto
            {
                Id = saved.Id,
                Name = saved.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear categoría");
            return new ObjectResult(new { error = "Error interno del servidor" }) { StatusCode = 500 };
        }
    }
}
