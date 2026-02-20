// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PalGoAPI.Models;

namespace PalGoAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add DbSet for RefreshTokens
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure boolean properties for Oracle
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.IsActive)
                    .HasConversion<int>();
                entity.Property(e => e.EmailConfirmed)
                    .HasConversion<int>();
                entity.Property(e => e.PhoneNumberConfirmed)
                    .HasConversion<int>();
                entity.Property(e => e.TwoFactorEnabled)
                    .HasConversion<int>();
                entity.Property(e => e.LockoutEnabled)
                    .HasConversion<int>();
            });

            // Configure RefreshToken entity
            builder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");

                entity.HasKey(e => e.Id);

                // UserId is string (Identity GUID)
                entity.Property(e => e.UserId)
                    .HasColumnName("UserId")
                    .IsRequired()
                    .HasMaxLength(450); // Identity default

                // Token - unique index
                entity.Property(e => e.Token)
                    .HasColumnName("Token")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasIndex(e => e.Token)
                    .IsUnique()
                    .HasDatabaseName("IX_RefreshTokens_Token");

                entity.Property(e => e.ExpiresAt)
                    .HasColumnName("ExpiresAt")
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CreatedAt")
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.RevokedAt)
                    .HasColumnName("RevokedAt")
                    .IsRequired(false);

                entity.Property(e => e.DeviceInfo)
                    .HasColumnName("DeviceInfo")
                    .HasMaxLength(255)
                    .IsRequired(false);

                entity.Property(e => e.IpAddress)
                    .HasColumnName("IpAddress")
                    .HasMaxLength(50)
                    .IsRequired(false);

                // Foreign key to AspNetUsers (Identity table)
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .HasConstraintName("FK_RefreshToken_User")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_RefreshTokens_UserId");
            });
        }
    }
}