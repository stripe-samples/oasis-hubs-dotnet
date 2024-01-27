using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OasisHubs.Site.Data;

public class OasisHubsDbContext : IdentityDbContext<OasisHubsUser> {
   public OasisHubsDbContext(DbContextOptions<OasisHubsDbContext> options)
       : base(options) {
   }

   public DbSet<HubRental> HubRentals  => Set<HubRental>();
   public DbSet<Booking> Bookings  => Set<Booking>();

   protected override void OnModelCreating(ModelBuilder modelBuilder) {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<OasisHubsUser>(e => {
         e.Property(p => p.StripeCustomerId)
             .HasMaxLength(100);

         e.Property(p => p.StripeAccountId)
             .HasMaxLength(100);

         e.Property(p => p.IsHost)
             .HasMaxLength(50)
             .HasDefaultValue(false);

         e.HasMany(e => e.Claims)
            .WithOne()
            .HasForeignKey(uc => uc.UserId)
            .IsRequired();
      });

      modelBuilder.Entity<HubRental>(e => {
         e.Property(p => p.HubType)
             .HasConversion<string>();


         e.HasKey(p => p.Id);
      });
   }
}
