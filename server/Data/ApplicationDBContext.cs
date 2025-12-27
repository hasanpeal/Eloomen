using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace server.Models;

public class ApplicationDBContext : IdentityDbContext<User>
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        : base(options)
    {
    }

    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Customize Identity table names to remove "AspNet" prefix
        builder.Entity<User>(entity => entity.ToTable("Users"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>(entity => entity.ToTable("Roles"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));
        
        List<IdentityRole> roles = new List<IdentityRole>
        {
            new IdentityRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = "2", Name = "User", NormalizedName = "USER" }
        };
        builder.Entity<IdentityRole>().HasData(roles);
        
        // UserDevices configuration
        builder.Entity<UserDevice>(entity =>
        {
            entity.ToTable("UserDevices");
            entity.HasIndex(e => new { e.UserId, e.DeviceIdentifier }).IsUnique();
            entity.HasIndex(e => e.UserId);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            
            entity.HasMany(e => e.RefreshTokens)
                .WithOne(e => e.UserDevice)
                .HasForeignKey(e => e.UserDeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // RefreshTokens configuration
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserDeviceId);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.UserDevice)
                .WithMany(e => e.RefreshTokens)
                .HasForeignKey(e => e.UserDeviceId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // VerificationCodes configuration
        builder.Entity<VerificationCode>(entity =>
        {
            entity.ToTable("VerificationCodes");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.Purpose });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CodeHash);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
        
        
    }

    // Add DbSet properties for your entities here
    // Example:
    // public DbSet<User> Users { get; set; }
    // public DbSet<Vault> Vaults { get; set; }
}

