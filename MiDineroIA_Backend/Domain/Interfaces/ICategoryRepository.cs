using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Repositorio para operaciones de categorías.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Obtiene todas las categorías disponibles para un usuario.
    /// Incluye categorías del sistema (UserId == null) y personalizadas del usuario.
    /// </summary>
    /// <param name="userId">Id del usuario.</param>
    /// <returns>Lista de categorías con su CategoryGroup cargado.</returns>
    Task<List<Category>> GetByUserAsync(int userId);

    /// <summary>
    /// Obtiene las categorías agrupadas en formato JSON para inyectar en el prompt de Claude.
    /// Formato: { "groups": [{ "name": "...", "type": "INGRESO|EGRESO", "categories": [{ "id": 1, "name": "..." }] }] }
    /// </summary>
    /// <param name="userId">Id del usuario.</param>
    /// <returns>String JSON con las categorías agrupadas.</returns>
    Task<string> GetCategoriesAsJsonAsync(int userId);

    /// <summary>
    /// Crea una categoría personalizada para un usuario.
    /// </summary>
    /// <param name="category">Categoría a crear.</param>
    /// <returns>Categoría creada con Id asignado.</returns>
    Task<Category> CreateAsync(Category category);
}
