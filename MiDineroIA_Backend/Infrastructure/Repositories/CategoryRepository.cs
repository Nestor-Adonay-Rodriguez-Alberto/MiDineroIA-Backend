using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;

namespace MiDineroIA_Backend.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }



    // OBTENER CATEGORÍAS ACTIVAS PARA UN USUARIO (INCLUYE CATEGORÍAS GLOBALES Y DEL USUARIO)
    public async Task<List<Category>> GetByUserAsync(int userId)
    {
        return await _context.Categories
            .Include(c => c.CategoryGroup)
            .Where(c => c.IsActive && (c.UserId == null || c.UserId == userId))
            .OrderBy(c => c.CategoryGroup.DisplayOrder)
            .ThenBy(c => c.DisplayOrder)
            .ToListAsync();
    }


    // OBTENER CATEGORÍAS AGRUPADAS POR GRUPO EN FORMATO JSON
    public async Task<string> GetCategoriesAsJsonAsync(int userId)
    {
        var categories = await GetByUserAsync(userId);

        var groups = categories
            .GroupBy(c => c.CategoryGroup)
            .OrderBy(g => g.Key.DisplayOrder)
            .Select(g => new
            {
                name = g.Key.Name,
                type = g.Key.TransactionType,
                categories = g.Select(c => new
                {
                    id = c.Id,
                    name = c.Name
                }).ToList()
            })
            .ToList();

        var result = new { groups };
        return JsonSerializer.Serialize(result, JsonOptions);
    }


    // CREA UNA NUEVA CATEGORÍA PERSONALIZADA PARA EL USUARIO:
    public async Task<Category> CreateAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }


}
