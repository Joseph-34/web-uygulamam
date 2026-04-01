using System.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using kazandakazan.Data;
using kazandakazan.Hubs;
using kazandakazan.Models;
using kazandakazan.Models.Entities;
using kazandakazan.Models.ViewModels;

namespace kazandakazan.Services;

public class PotService(
    ApplicationDbContext db,
    IHubContext<PotHub> hub,
    IWinnerSmsNotifier winnerSmsNotifier) : IPotService
{
    /// <summary>Tek seferde kumbaraya atılabilen sabit tutarlar.</summary>
    public static readonly decimal[] ContributeChipAmounts = [5, 10, 20, 50, 100, 200, 500, 1000, 10_000];

    /// <summary>Demo cüzdana eklenebilecek tutarlar.</summary>
    public static readonly decimal[] DemoChipAmounts = [5, 10, 20, 50, 100, 200, 500, 1000, 10_000];

    private static readonly HashSet<decimal> ContributeChipSet = [..ContributeChipAmounts];
    private static readonly HashSet<decimal> DemoChipSet = [..DemoChipAmounts];

    private const decimal TicketUnit = 5m;
    private const decimal MaxSimulatedBankDeposit = 500_000m;

    public async Task<PotStateDto?> GetPotStateAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        var pot = await db.Pots
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(p => p.Status == PotStatus.Open, cancellationToken);

        if (pot is null)
            return null;

        return await BuildStateAsync(pot.Id, userId, cancellationToken);
    }

    public async Task<ContributeResult> ContributeAsync(Guid userId, int potId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (!ContributeChipSet.Contains(amount))
            return ContributeResult.Fail("Geçersiz tutar.");

        return await RunContributionAsync(userId, potId, amount, cancellationToken);
    }

    public async Task<ContributeResult> ContributeAllAsync(Guid userId, int potId, decimal reserveAmount, CancellationToken cancellationToken = default)
    {
        if (reserveAmount < 0)
            return ContributeResult.Fail("Geçersiz rezerv.");

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            decimal contributed = 0;
            PotWinnerInfo? winInfo = null;

            try
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (user is null)
                    return ContributeResult.Fail("Oturum hatası.");

                var pot = await db.Pots.FirstOrDefaultAsync(p => p.Id == potId, cancellationToken);
                if (pot is null || pot.Status != PotStatus.Open)
                    return ContributeResult.Fail("Tur kapalı.");

                var remaining = pot.TargetAmount - pot.CurrentBalance;
                if (remaining <= 0)
                    return ContributeResult.Fail("Hedef dolu.");

                var available = user.WalletBalance - reserveAmount;
                var toThrow = Math.Floor(available / TicketUnit) * TicketUnit;
                if (toThrow < TicketUnit)
                    return ContributeResult.Fail("Bakiye yetersiz.");

                var cap = Math.Floor(remaining / TicketUnit) * TicketUnit;
                toThrow = Math.Min(toThrow, cap);
                if (toThrow < TicketUnit)
                    return ContributeResult.Fail("Aktarılamıyor.");

                contributed = toThrow;
                winInfo = await ApplyContributionCoreAsync(user, pot, userId, toThrow, cancellationToken);

                await tx.CommitAsync(cancellationToken);
                await AfterPotMutationAsync(winInfo, cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            return winInfo is null
                ? ContributeResult.Ok(actualAmount: contributed)
                : ContributeResult.Ok(winInfo.UserName, contributed);
        });
    }

    private async Task<ContributeResult> RunContributionAsync(Guid userId, int potId, decimal amount, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            PotWinnerInfo? winInfo = null;
            decimal contributed = 0;

            try
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                if (user is null)
                    return ContributeResult.Fail("Oturum hatası.");

                var pot = await db.Pots.FirstOrDefaultAsync(p => p.Id == potId, cancellationToken);
                if (pot is null || pot.Status != PotStatus.Open)
                    return ContributeResult.Fail("Tur kapalı.");

                var remaining = pot.TargetAmount - pot.CurrentBalance;
                if (remaining <= 0)
                    return ContributeResult.Fail("Hedef dolu.");

                var capped = Math.Floor(Math.Min(amount, remaining) / TicketUnit) * TicketUnit;
                if (capped < TicketUnit)
                    return ContributeResult.Fail("Aktarılamıyor.");

                if (user.WalletBalance < capped)
                    return ContributeResult.Fail("Bakiye yetersiz.");

                contributed = capped;
                winInfo = await ApplyContributionCoreAsync(user, pot, userId, capped, cancellationToken);

                await tx.CommitAsync(cancellationToken);
                await AfterPotMutationAsync(winInfo, cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            return winInfo is null
                ? ContributeResult.Ok(actualAmount: contributed)
                : ContributeResult.Ok(winInfo.UserName, contributed);
        });
    }

    private async Task AfterPotMutationAsync(PotWinnerInfo? winner, CancellationToken cancellationToken)
    {
        await BroadcastStateAsync(cancellationToken);
        if (winner is null)
            return;

        await hub.Clients.All.SendAsync("winnerAnnounced", new { userName = winner.UserName }, cancellationToken);
        await winnerSmsNotifier.NotifyWinnerAsync(winner, cancellationToken);
    }

    private async Task<PotWinnerInfo?> ApplyContributionCoreAsync(
        ApplicationUser user,
        Pot pot,
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        user.WalletBalance -= amount;
        pot.CurrentBalance += amount;

        var txn = new PotTransaction
        {
            UserId = userId,
            PotId = pot.Id,
            Amount = amount,
            Status = PaymentStatus.Completed,
            CreatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };
        db.PotTransactions.Add(txn);
        await db.SaveChangesAsync(cancellationToken);

        var ticketCount = (int)(amount / TicketUnit);
        var now = DateTime.UtcNow;
        for (var i = 0; i < ticketCount; i++)
        {
            db.Tickets.Add(new Ticket
            {
                UserId = userId,
                PotId = pot.Id,
                PotTransactionId = txn.Id,
                CreatedAtUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        if (pot.CurrentBalance >= pot.TargetAmount)
            return await ClosePotAndOpenNextAsync(pot, cancellationToken);

        return null;
    }

    public async Task<(bool ok, string message)> DemoAddWalletAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (!DemoChipSet.Contains(amount))
            return (false, "Geçersiz tutar.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return (false, "Oturum hatası.");

        // Deneme ortamı: günlük demo yükleme üst sınırı kapalı.
        user.WalletBalance += amount;
        await db.SaveChangesAsync(cancellationToken);
        await BroadcastStateAsync(cancellationToken);

        return (true, $"+{amount:N0} ₺");
    }

    public async Task<(bool ok, string message)> SimulatedBankDepositAsync(
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount < TicketUnit || amount > MaxSimulatedBankDeposit)
            return (false, $"5–{MaxSimulatedBankDeposit:N0} ₺");

        if (amount % TicketUnit != 0)
            return (false, "5 ₺ katı girin.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return (false, "Oturum hatası.");

        user.WalletBalance += amount;
        await db.SaveChangesAsync(cancellationToken);
        await BroadcastStateAsync(cancellationToken);

        return (true, $"+{amount:N0} ₺");
    }

    public async Task BroadcastStateAsync(CancellationToken cancellationToken = default)
    {
        var openPot = await db.Pots
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(p => p.Status == PotStatus.Open, cancellationToken);

        PotStateDto? dto = openPot is null
            ? null
            : await BuildStateAsync(openPot.Id, null, cancellationToken);

        await hub.Clients.All.SendAsync("potUpdated", dto, cancellationToken: cancellationToken);
    }

    public async Task<LastWinnerInfo?> GetLastWinnerAsync(CancellationToken cancellationToken = default)
    {
        var pot = await db.Pots.AsNoTracking()
            .Where(p => p.Status == PotStatus.Closed && p.WinnerUserId != null && p.ClosedAtUtc != null)
            .OrderByDescending(p => p.ClosedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (pot?.WinnerUserId is null || pot.ClosedAtUtc is null)
            return null;

        var name = await db.Users.AsNoTracking()
            .Where(u => u.Id == pot.WinnerUserId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync(cancellationToken);

        return new LastWinnerInfo(name ?? "Kullanıcı", pot.ClosedAtUtc.Value);
    }

    private async Task<PotWinnerInfo?> ClosePotAndOpenNextAsync(Pot pot, CancellationToken cancellationToken)
    {
        var tickets = await db.Tickets.Where(t => t.PotId == pot.Id).ToListAsync(cancellationToken);
        if (tickets.Count == 0)
        {
            pot.Status = PotStatus.Closed;
            pot.ClosedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        var win = tickets[Random.Shared.Next(tickets.Count)];
        var winner = await db.Users.FirstAsync(u => u.Id == win.UserId, cancellationToken);
        var prize = pot.TargetAmount;

        pot.WinnerUserId = win.UserId;
        pot.Status = PotStatus.Closed;
        pot.ClosedAtUtc = DateTime.UtcNow;
        pot.CurrentBalance = pot.TargetAmount;

        winner.WalletBalance += pot.TargetAmount;

        var next = new Pot
        {
            DisplayName = pot.DisplayName ?? "Büyük kumbara",
            CurrentBalance = 0,
            TargetAmount = pot.TargetAmount,
            Status = PotStatus.Open,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Pots.Add(next);
        await db.SaveChangesAsync(cancellationToken);
        return new PotWinnerInfo(winner.UserName ?? "Kullanıcı", winner.PhoneNumber, prize);
    }

    private async Task<PotStateDto> BuildStateAsync(int potId, Guid? userId, CancellationToken cancellationToken)
    {
        var pot = await db.Pots.AsNoTracking().FirstAsync(p => p.Id == potId, cancellationToken);
        var totalTickets = await db.Tickets.AsNoTracking().LongCountAsync(t => t.PotId == potId, cancellationToken);

        string? winnerName = null;
        if (pot.WinnerUserId.HasValue && pot.Status == PotStatus.Closed)
        {
            winnerName = await db.Users.AsNoTracking()
                .Where(u => u.Id == pot.WinnerUserId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        decimal? wallet = null;
        long? userTickets = null;
        double? chance = null;
        if (userId.HasValue)
        {
            var u = await db.Users.AsNoTracking()
                .Where(x => x.Id == userId.Value)
                .Select(x => new { x.WalletBalance })
                .FirstOrDefaultAsync(cancellationToken);

            if (u is not null)
                wallet = u.WalletBalance;

            userTickets = await db.Tickets.AsNoTracking()
                .LongCountAsync(t => t.PotId == potId && t.UserId == userId, cancellationToken);

            chance = totalTickets == 0 ? 0 : 100.0 * userTickets.Value / totalTickets;
        }

        var fill = pot.TargetAmount <= 0
            ? 0
            : Math.Min(100, (double)(pot.CurrentBalance / pot.TargetAmount) * 100);

        return new PotStateDto(
            pot.Id,
            pot.CurrentBalance,
            pot.TargetAmount,
            fill,
            pot.Status == PotStatus.Open,
            winnerName,
            totalTickets,
            wallet,
            userTickets,
            chance,
            null,
            null);
    }
}
