using Microsoft.AspNetCore.Mvc;
using TransactionPratice.Application.Services;

namespace TransactionPratice.Controllers;

public sealed class TransferController(ITransferService transferService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Accounts = await transferService.GetAccountsAsync(cancellationToken);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchTest(
        int fromAccountId,
        int toAccountId,
        decimal amount,
        int loopCount,
        int failAt,
        CancellationToken cancellationToken)
    {
        var result = await transferService.BatchTransferWithRollbackTestAsync(
            fromAccountId,
            toAccountId,
            amount,
            loopCount,
            failAt,
            cancellationToken);

        ViewBag.Message = result.Message;
        ViewBag.Success = result.Success;
        ViewBag.InputFromAccountId = fromAccountId;
        ViewBag.InputToAccountId = toAccountId;
        ViewBag.InputAmount = amount;
        ViewBag.InputLoopCount = loopCount;
        ViewBag.InputFailAt = failAt;
        ViewBag.Accounts = await transferService.GetAccountsAsync(cancellationToken);
        return View("Index");
    }
}
