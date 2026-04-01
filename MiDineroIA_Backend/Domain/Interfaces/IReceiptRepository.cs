using MiDineroIA_Backend.Domain.Entities;

namespace MiDineroIA_Backend.Domain.Interfaces;

/// <summary>
/// Repositorio para operaciones de recibos/facturas.
/// </summary>
public interface IReceiptRepository
{
    /// <summary>
    /// Crea un nuevo recibo asociado a una transacción.
    /// </summary>
    /// <param name="receipt">Recibo a crear.</param>
    /// <returns>Recibo creado con Id asignado.</returns>
    Task<Receipt> CreateAsync(Receipt receipt);

    /// <summary>
    /// Obtiene un recibo por el Id de su transacción.
    /// </summary>
    /// <param name="transactionId">Id de la transacción asociada.</param>
    /// <returns>Recibo encontrado o null.</returns>
    Task<Receipt?> GetByTransactionIdAsync(int transactionId);
}
