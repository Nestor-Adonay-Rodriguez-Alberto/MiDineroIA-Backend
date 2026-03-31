using Microsoft.EntityFrameworkCore;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Domain.Interfaces;
using MiDineroIA_Backend.Infrastructure.Database;

namespace MiDineroIA_Backend.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }



    // CREA UNA NUEVA TRANSACCIÓN EN LA BASE DE DATOS:
    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }


    // OBTIENE TODAS LAS TRANSACCIONES DE UN USUARIO:
    public async Task<Transaction?> GetByIdAsync(int id, int userId)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }


    // ACTUALIZA UNA TRANSACCIÓN EXISTENTE EN LA BASE DE DATOS:
    public async Task<Transaction> UpdateAsync(Transaction transaction)
    {
        transaction.UpdatedAt = DateTime.UtcNow;
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }


    // ELIMINA UNA TRANSACCIÓN DE LA BASE DE DATOS:
    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return false;

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return true;
    }


    // CONFIRMA UNA TRANSACCIÓN (MARCARLA COMO CONFIRMADA):
    public async Task<bool> ConfirmAsync(int id, int userId)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
            return false;

        transaction.IsConfirmed = true;
        transaction.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }


}
