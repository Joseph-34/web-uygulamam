namespace kazandakazan.Models.ViewModels;

public record PotStateDto(
    int PotId,
    decimal CurrentBalance,
    decimal TargetAmount,
    double FillPercent,
    bool IsOpen,
    string? WinnerUserName,
    long TotalTickets,
    decimal? UserWallet,
    long? UserTicketCount,
    double? UserWinChancePercent,
    decimal? DailyDemoLoadRemaining = null,
    decimal? DailyDemoLoadLimit = null);
