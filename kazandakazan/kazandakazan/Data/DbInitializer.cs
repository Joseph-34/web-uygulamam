using Microsoft.EntityFrameworkCore;
using kazandakazan.Models.Entities;

namespace kazandakazan.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Pots.AnyAsync(cancellationToken))
        {
            db.Pots.Add(new Pot
            {
                DisplayName = "Büyük ödül kumbarası",
                CurrentBalance = 0,
                TargetAmount = 100_000,
                Status = PotStatus.Open,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Varsayılan kumbara oluşturuldu (hedef 100.000 ₺).");
        }
    }
}
