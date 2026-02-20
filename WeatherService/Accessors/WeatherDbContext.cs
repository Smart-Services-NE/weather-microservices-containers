using Microsoft.EntityFrameworkCore;
using WeatherService.Contracts;

namespace WeatherService.Accessors;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

    public DbSet<SubscriptionRecord> Subscriptions => Set<SubscriptionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.ZipCode).IsRequired();
        });
    }
}
