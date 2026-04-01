namespace kazandakazan.Models;

/// <summary>Çekiliş sonrası kazanan bilgisi (SMS ve bildirim için).</summary>
public sealed record PotWinnerInfo(string UserName, string? PhoneNumber, decimal PrizeTry);
