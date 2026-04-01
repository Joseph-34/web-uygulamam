using kazandakazan.Models;

namespace kazandakazan.Models.Entities;

/// <summary>
/// Kullanıcının kumbaraya yaptığı ödeme / yatırım kaydı.
/// </summary>
public class PotTransaction
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int PotId { get; set; }

    public Pot Pot { get; set; } = null!;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    /// <summary>Sanal POS / ödeme sağlayıcı işlem kimliği (tekrarlı webhook için).</summary>
    public string? ExternalPaymentId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
