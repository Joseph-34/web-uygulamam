using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using kazandakazan.Models;
using kazandakazan.Models.Entities;

namespace kazandakazan.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Pot> Pots => Set<Pot>();

    public DbSet<PotTransaction> PotTransactions => Set<PotTransaction>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.WalletBalance).HasPrecision(18, 2);
            e.Property(x => x.WalletDailyTopUpUsedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Pot>(e =>
        {
            e.ToTable("Pots");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(256);
            e.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            e.Property(x => x.TargetAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<byte>();
            e.HasOne(x => x.Winner)
                .WithMany(u => u.WonPots)
                .HasForeignKey(x => x.WinnerUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PotTransaction>(e =>
        {
            e.ToTable("PotTransactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.ExternalPaymentId).HasMaxLength(128);
            e.HasIndex(x => new { x.PotId, x.ExternalPaymentId })
                .IsUnique()
                .HasFilter("[ExternalPaymentId] IS NOT NULL");
            e.HasIndex(x => x.PotId);
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Pot)
                .WithMany(p => p.Transactions)
                .HasForeignKey(x => x.PotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("Tickets");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PotId);
            e.HasIndex(x => new { x.UserId, x.PotId });
            e.HasOne(x => x.User)
                .WithMany(u => u.Tickets)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Pot)
                .WithMany(p => p.Tickets)
                .HasForeignKey(x => x.PotId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PotTransaction)
                .WithMany(t => t.Tickets)
                .HasForeignKey(x => x.PotTransactionId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
