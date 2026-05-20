using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sferity.Backend.Models;

namespace Sferity.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<PromoCode> PromoCodes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PromoCode>()
                .HasIndex(p => p.Code)
                .IsUnique();
            

            modelBuilder.Entity<PromoCode>()
                .Property(p => p.Status)
                .HasConversion(new EnumToStringConverter<PromoCodeStatus>());
            
        }
    }
}