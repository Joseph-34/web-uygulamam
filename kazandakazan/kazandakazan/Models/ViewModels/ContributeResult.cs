namespace kazandakazan.Models.ViewModels;

public sealed class ContributeResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? WinnerUserName { get; init; }

    /// <summary>Kasaya aktarılan gerçek tutar (kalan hedef &lt; seçilen chip ise kısıtlanır).</summary>
    public decimal? ActualAmount { get; init; }

    public static ContributeResult Ok(string? winnerUserName = null, decimal? actualAmount = null) =>
        new() { Success = true, WinnerUserName = winnerUserName, ActualAmount = actualAmount };

    public static ContributeResult Fail(string message) =>
        new() { Success = false, Message = message };
}
