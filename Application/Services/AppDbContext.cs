using Microsoft.EntityFrameworkCore;

namespace TransactionPratice.Application.Services;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AccountItem> Accounts => Set<AccountItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountItem>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OwnerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Balance).HasColumnType("decimal(18,2)");
        });
    }
}
