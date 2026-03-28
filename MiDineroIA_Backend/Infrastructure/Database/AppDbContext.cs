using Microsoft.EntityFrameworkCore;
using MiDineroIA_Backend.Domain.Entities;
using MiDineroIA_Backend.Infrastructure.Configurations;

namespace MiDineroIA_Backend.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<CategoryGroup> CategoryGroups { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<MonthlyBudget> MonthlyBudgets { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Receipt> Receipts { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryGroupConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new MonthlyBudgetConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new ReceiptConfiguration());
    }

}
