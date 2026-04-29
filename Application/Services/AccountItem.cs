namespace TransactionPratice.Application.Services;

public sealed class AccountItem
{
    public int Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
