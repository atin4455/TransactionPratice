using Microsoft.EntityFrameworkCore;

namespace TransactionPratice.Application.Services;

public sealed class TransferService(AppDbContext dbContext) : ITransferService
{
    public async Task<IReadOnlyList<AccountItem>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        await EnsureSchemaAndSeedAsync(cancellationToken);
        return await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message)> TransferAsync(int fromAccountId, int toAccountId, decimal amount, CancellationToken cancellationToken)
    {
        if (fromAccountId == toAccountId)
            return (false, "轉出與轉入帳戶不能相同");
        if (amount <= 0)
            return (false, "金額需大於 0");

        await EnsureSchemaAndSeedAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var from = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == fromAccountId, cancellationToken);
            var to = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == toAccountId, cancellationToken);

            if (from is null || to is null)
                return (false, "帳戶不存在");
            if (from.Balance < amount)
                return (false, "餘額不足");

            from.Balance -= amount;
            to.Balance += amount;
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return (true, "轉帳成功（EF Transaction Commit）");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "轉帳失敗（EF Transaction Rollback）");
        }
    }

    public async Task<(bool Success, string Message)> BatchTransferWithRollbackTestAsync(
        int fromAccountId,
        int toAccountId,
        decimal amount,
        int loopCount,
        int failAt,
        CancellationToken cancellationToken)
    {
        if (fromAccountId == toAccountId)
            return (false, "轉出與轉入帳戶不能相同");
        if (amount <= 0)
            return (false, "金額需大於 0");
        if (loopCount <= 0)
            return (false, "回圈次數需大於 0");
        if (failAt < 0 || failAt > loopCount)
            return (false, "中斷點需為 0（不中斷）或 1~回圈次數");

        await EnsureSchemaAndSeedAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            for (int i = 1; i <= loopCount; i++)
            {
                var from = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == fromAccountId, cancellationToken);
                var to = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == toAccountId, cancellationToken);

                if (from is null || to is null)
                    throw new InvalidOperationException("帳戶不存在");
                if (from.Balance < amount)
                    throw new InvalidOperationException($"第 {i} 筆失敗：餘額不足");

                from.Balance -= amount;
                to.Balance += amount;
                await dbContext.SaveChangesAsync(cancellationToken);

                if (failAt > 0 && i == failAt)
                {
                    // 模擬中途斷線/例外，驗證整批 rollback
                    throw new Exception($"模擬中斷於第 {i} 筆");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, $"批次成功：共 {loopCount} 筆，已 Commit");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"批次失敗：{ex.Message}，整批已 Rollback");
        }
    }

    private async Task EnsureSchemaAndSeedAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        if (await dbContext.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Accounts.AddRange(
            new AccountItem { Id = 1, OwnerName = "Alice", Balance = 1000 },
            new AccountItem { Id = 2, OwnerName = "Bob", Balance = 500 },
            new AccountItem { Id = 3, OwnerName = "Carol", Balance = 300 });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
