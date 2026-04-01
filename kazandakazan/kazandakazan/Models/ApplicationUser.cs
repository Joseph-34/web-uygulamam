using Microsoft.AspNetCore.Identity;

namespace kazandakazan.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Simülasyon cüzdan bakiyesi (gerçek para değildir).</summary>
    public decimal WalletBalance { get; set; }

    /// <summary>Türkiye takvim günü; ileride günlük demo limiti için kullanılabilir.</summary>
    public DateOnly? WalletDailyTopUpDate { get; set; }

    /// <summary>O gün yüklenen demo tutarı; şu an limit uygulanmıyor.</summary>
    public decimal WalletDailyTopUpUsedAmount { get; set; }

    public DateTime? AcceptedTermsAtUtc { get; set; }

    public ICollection<Entities.PotTransaction> Transactions { get; set; } = new List<Entities.PotTransaction>();

    public ICollection<Entities.Ticket> Tickets { get; set; } = new List<Entities.Ticket>();

    public ICollection<Entities.Pot> WonPots { get; set; } = new List<Entities.Pot>();
}
