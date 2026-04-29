namespace TransactionPratice.Application.Services;

public interface ITransferService
{
    Task<IReadOnlyList<AccountItem>> GetAccountsAsync(CancellationToken cancellationToken);
    Task<(bool Success, string Message)> TransferAsync(int fromAccountId, int toAccountId, decimal amount, CancellationToken cancellationToken);
    Task<(bool Success, string Message)> BatchTransferWithRollbackTestAsync(
        int fromAccountId,
        int toAccountId,
        decimal amount,
        int loopCount,
        int failAt,
        CancellationToken cancellationToken);
}
