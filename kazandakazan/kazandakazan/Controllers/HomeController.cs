using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using kazandakazan.Models;
using kazandakazan.Models.ViewModels;
using kazandakazan.Services;

namespace kazandakazan.Controllers;

public class HomeController(IPotService potService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var uid = GetUserId(User);
        var state = await potService.GetPotStateAsync(uid, cancellationToken);
        var lastWinner = await potService.GetLastWinnerAsync(cancellationToken);
        return View(new HomeDashboardViewModel { Pot = state, LastWinner = lastWinner });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContributeAjax([FromForm] int potId, [FromForm] decimal amount, CancellationToken cancellationToken)
    {
        var uid = GetUserId(User);
        if (uid is null)
            return Unauthorized();

        var result = await potService.ContributeAsync(uid.Value, potId, amount, cancellationToken);
        return Json(await ToAjaxAsync(uid.Value, result, amount, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContributeAllAjax([FromForm] int potId, [FromForm] decimal reserveAmount, CancellationToken cancellationToken)
    {
        var uid = GetUserId(User);
        if (uid is null)
            return Unauthorized();

        var result = await potService.ContributeAllAsync(uid.Value, potId, reserveAmount, cancellationToken);
        return Json(await ToAjaxAsync(uid.Value, result, requestedChipAmount: null, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemoWalletAjax([FromForm] decimal amount, CancellationToken cancellationToken)
    {
        var uid = GetUserId(User);
        if (uid is null)
            return Unauthorized();

        var (ok, message) = await potService.DemoAddWalletAsync(uid.Value, amount, cancellationToken);
        var pot = await potService.GetPotStateAsync(uid.Value, cancellationToken);
        return Json(new PotAjaxResponse
        {
            Success = ok,
            Message = message,
            Pot = pot
        });
    }

    /// <summary>Simüle edilmiş banka hesabından cüzdana yükleme (gerçek ödeme yoktur).</summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankDepositSimulateAjax(
        [FromForm] decimal amount,
        [FromForm] string? bankAccountLabel,
        CancellationToken cancellationToken)
    {
        var uid = GetUserId(User);
        if (uid is null)
            return Unauthorized();

        var (ok, message) = await potService.SimulatedBankDepositAsync(uid.Value, amount, cancellationToken);
        if (ok && !string.IsNullOrWhiteSpace(bankAccountLabel))
            message += " · " + bankAccountLabel.Trim();

        var pot = await potService.GetPotStateAsync(uid.Value, cancellationToken);
        return Json(new PotAjaxResponse
        {
            Success = ok,
            Message = message,
            Pot = pot
        });
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PotStateJson(CancellationToken cancellationToken)
    {
        var uid = GetUserId(User);
        var state = await potService.GetPotStateAsync(uid, cancellationToken);
        return Json(state);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private async Task<PotAjaxResponse> ToAjaxAsync(
        Guid userId,
        ContributeResult result,
        decimal? requestedChipAmount,
        CancellationToken cancellationToken)
    {
        var pot = await potService.GetPotStateAsync(userId, cancellationToken);
        string? msg;
        if (!result.Success)
            msg = result.Message;
        else if (!string.IsNullOrEmpty(result.WinnerUserName))
            msg = $"Kazanan: {result.WinnerUserName}";
        else if (requestedChipAmount is decimal req
                 && result.ActualAmount is decimal act
                 && act < req)
            msg = $"{act:N0} ₺ aktarıldı.";
        else
            msg = "Aktarıldı.";

        return new PotAjaxResponse
        {
            Success = result.Success,
            Message = msg,
            WinnerUserName = result.WinnerUserName,
            Pot = pot
        };
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
