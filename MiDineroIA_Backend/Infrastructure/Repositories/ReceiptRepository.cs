using Microsoft.EntityFrameworkCore;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;

namespace MiDineroIA_Backend.Infrastructure.Repositories;

/// <summary>
/// Repositorio para operaciones de recibos/facturas.
/// </summary>
public class ReceiptRepository : IReceiptRepository
{
    private readonly AppDbContext _context;

    public ReceiptRepository(AppDbContext context)
    {
        _context = context;
    }


    public async Task<Receipt> CreateAsync(Receipt receipt)
    {
        receipt.CreatedAt = DateTime.UtcNow;
        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();
        return receipt;
    }


    public async Task<Receipt?> GetByTransactionIdAsync(int transactionId)
    {
        return await _context.Receipts
            .FirstOrDefaultAsync(r => r.TransactionId == transactionId);
    }
}
