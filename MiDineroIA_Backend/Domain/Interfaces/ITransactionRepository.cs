using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Repositorio para operaciones de transacciones (gastos e ingresos).
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Crea una nueva transacción.
    /// </summary>
    /// <param name="transaction">Transacción a crear.</param>
    /// <returns>Transacción creada con Id asignado.</returns>
    Task<Transaction> CreateAsync(Transaction transaction);

    /// <summary>
    /// Obtiene una transacción por su Id.
    /// SIEMPRE filtra por userId para seguridad multi-tenant.
    /// </summary>
    /// <param name="id">Id de la transacción.</param>
    /// <param name="userId">Id del usuario dueño.</param>
    /// <returns>Transacción encontrada o null.</returns>
    Task<Transaction?> GetByIdAsync(int id, int userId);

    /// <summary>
    /// Actualiza una transacción existente.
    /// </summary>
    /// <param name="transaction">Transacción con datos actualizados.</param>
    /// <returns>Transacción actualizada.</returns>
    Task<Transaction> UpdateAsync(Transaction transaction);

    /// <summary>
    /// Elimina una transacción.
    /// SIEMPRE filtra por userId para seguridad multi-tenant.
    /// </summary>
    /// <param name="id">Id de la transacción.</param>
    /// <param name="userId">Id del usuario dueño.</param>
    /// <returns>True si se eliminó, false si no existía.</returns>
    Task<bool> DeleteAsync(int id, int userId);

    /// <summary>
    /// Marca una transacción como confirmada (IsConfirmed = true).
    /// SIEMPRE filtra por userId para seguridad multi-tenant.
    /// </summary>
    /// <param name="id">Id de la transacción.</param>
    /// <param name="userId">Id del usuario dueño.</param>
    /// <returns>True si se confirmó, false si no existía.</returns>
    Task<bool> ConfirmAsync(int id, int userId);

    /// <summary>
    /// Obtiene el resumen de transacciones de un mes.
    /// </summary>
    /// <param name="userId">Id del usuario.</param>
    /// <param name="year">Año.</param>
    /// <param name="month">Mes (1-12).</param>
    /// <returns>Tupla con (totalIngresos, totalEgresos).</returns>
    Task<(decimal TotalIncome, decimal TotalExpenses)> GetMonthlySummaryAsync(int userId, int year, int month);
}
