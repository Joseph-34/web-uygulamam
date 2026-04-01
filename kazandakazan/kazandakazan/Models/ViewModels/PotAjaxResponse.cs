namespace kazandakazan.Models.ViewModels;

public class PotAjaxResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public string? WinnerUserName { get; init; }

    public PotStateDto? Pot { get; init; }
}
