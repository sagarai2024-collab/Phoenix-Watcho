using Microsoft.EntityFrameworkCore;
using PhoenixWatcho.Models;

namespace PhoenixWatcho.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<KnowledgeBase> KnowledgeBase => Set<KnowledgeBase>();
    public DbSet<ChatLog> ChatLogs => Set<ChatLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscriber>()
            .HasIndex(s => s.VcNumber)
            .IsUnique();

        modelBuilder.Entity<Plan>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<Plan>()
            .Property(p => p.MonthlyPrice).HasPrecision(10, 2);
        modelBuilder.Entity<Plan>()
            .Property(p => p.DiscountPercent).HasPrecision(5, 2);

        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Subscriber)
            .WithMany(sub => sub.Subscriptions)
            .HasForeignKey(s => s.SubscriberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Derived properties are not persisted
        modelBuilder.Entity<Subscription>().Ignore(s => s.DaysRemaining);
        modelBuilder.Entity<Subscription>().Ignore(s => s.Status);
    }
}
