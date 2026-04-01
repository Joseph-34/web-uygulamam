using kazandakazan.Models;

namespace kazandakazan.Models.Entities;

/// <summary>
/// Her bilet satırı bir çekiliş hakkıdır (ör. her 5 TL için bir satır).
/// </summary>
public class Ticket
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int PotId { get; set; }

    public Pot Pot { get; set; } = null!;

    public long? PotTransactionId { get; set; }

    public PotTransaction? PotTransaction { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
