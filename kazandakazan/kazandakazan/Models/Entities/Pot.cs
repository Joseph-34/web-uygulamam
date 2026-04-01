using kazandakazan.Models;

namespace kazandakazan.Models.Entities;

/// <summary>
/// Kumbara / fonlama turu. Hedef dolunca çekiliş kapanır.
/// </summary>
public class Pot
{
    public int Id { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>Mevcut toplanan tutar (tamamlanmış ödemeler).</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Hedef tutar (ör. 100_000).</summary>
    public decimal TargetAmount { get; set; }

    public PotStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    public Guid? WinnerUserId { get; set; }

    public ApplicationUser? Winner { get; set; }

    public ICollection<PotTransaction> Transactions { get; set; } = new List<PotTransaction>();

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
