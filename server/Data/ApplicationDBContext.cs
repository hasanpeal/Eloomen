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
    public DbSet<Vault> Vaults { get; set; }
    public DbSet<VaultInvite> VaultInvites { get; set; }
    public DbSet<VaultMember> VaultMembers { get; set; }
    public DbSet<VaultItem> VaultItems { get; set; }
    public DbSet<VaultItemVisibility> VaultItemVisibilities { get; set; }
    public DbSet<VaultDocument> VaultDocuments { get; set; }
    public DbSet<VaultPassword> VaultPasswords { get; set; }
    public DbSet<VaultNote> VaultNotes { get; set; }
    public DbSet<VaultLink> VaultLinks { get; set; }
    public DbSet<VaultCryptoWallet> VaultCryptoWallets { get; set; }

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

        // Vaults configuration
        builder.Entity<Vault>(entity =>
        {
            entity.ToTable("Vaults");
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DeletedAt);
            
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            
            entity.HasMany(e => e.Members)
                .WithOne(e => e.Vault)
                .HasForeignKey(e => e.VaultId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Invites)
                .WithOne(e => e.Vault)
                .HasForeignKey(e => e.VaultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VaultInvites configuration
        builder.Entity<VaultInvite>(entity =>
        {
            entity.ToTable("VaultInvites");
            entity.HasIndex(e => e.VaultId);
            entity.HasIndex(e => e.InviterId);
            entity.HasIndex(e => e.InviteeEmail);
            entity.HasIndex(e => e.InviteeId);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.Vault)
                .WithMany(e => e.Invites)
                .HasForeignKey(e => e.VaultId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            
            entity.HasOne(e => e.Inviter)
                .WithMany()
                .HasForeignKey(e => e.InviterId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            
            entity.HasOne(e => e.Invitee)
                .WithMany()
                .HasForeignKey(e => e.InviteeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // VaultMembers configuration
        builder.Entity<VaultMember>(entity =>
        {
            entity.ToTable("VaultMembers");
            entity.HasIndex(e => e.VaultId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.VaultId, e.UserId, e.Status });
            entity.HasIndex(e => e.Status);
            
            entity.HasOne(e => e.Vault)
                .WithMany(e => e.Members)
                .HasForeignKey(e => e.VaultId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            
            entity.HasOne(e => e.AddedBy)
                .WithMany()
                .HasForeignKey(e => e.AddedById)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.RemovedBy)
                .WithMany()
                .HasForeignKey(e => e.RemovedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // VaultItems configuration
        builder.Entity<VaultItem>(entity =>
        {
            entity.ToTable("VaultItems");
            entity.HasIndex(e => e.VaultId);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.ItemType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DeletedAt);
            
            entity.HasOne(e => e.Vault)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.VaultId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
            
            entity.HasOne(e => e.DeletedByUser)
                .WithMany()
                .HasForeignKey(e => e.DeletedBy)
                .OnDelete(DeleteBehavior.SetNull);
            
            // One-to-one relationships with specific item types
            entity.HasOne(e => e.Document)
                .WithOne(e => e.VaultItem)
                .HasForeignKey<VaultDocument>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Password)
                .WithOne(e => e.VaultItem)
                .HasForeignKey<VaultPassword>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Note)
                .WithOne(e => e.VaultItem)
                .HasForeignKey<VaultNote>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Link)
                .WithOne(e => e.VaultItem)
                .HasForeignKey<VaultLink>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.CryptoWallet)
                .WithOne(e => e.VaultItem)
                .HasForeignKey<VaultCryptoWallet>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VaultItemVisibilities configuration
        builder.Entity<VaultItemVisibility>(entity =>
        {
            entity.ToTable("VaultItemVisibilities");
            entity.HasIndex(e => e.VaultItemId);
            entity.HasIndex(e => e.VaultMemberId);
            entity.HasIndex(e => new { e.VaultItemId, e.VaultMemberId }).IsUnique();
            
            entity.HasOne(e => e.VaultItem)
                .WithMany(e => e.Visibilities)
                .HasForeignKey(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            
            entity.HasOne(e => e.VaultMember)
                .WithMany()
                .HasForeignKey(e => e.VaultMemberId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // VaultDocuments configuration
        builder.Entity<VaultDocument>(entity =>
        {
            entity.ToTable("VaultDocuments");
            entity.HasIndex(e => e.ObjectKey);
            
            entity.HasOne(e => e.VaultItem)
                .WithOne(e => e.Document)
                .HasForeignKey<VaultDocument>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // VaultPasswords configuration
        builder.Entity<VaultPassword>(entity =>
        {
            entity.ToTable("VaultPasswords");
            
            entity.HasOne(e => e.VaultItem)
                .WithOne(e => e.Password)
                .HasForeignKey<VaultPassword>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // VaultNotes configuration
        builder.Entity<VaultNote>(entity =>
        {
            entity.ToTable("VaultNotes");
            
            entity.HasOne(e => e.VaultItem)
                .WithOne(e => e.Note)
                .HasForeignKey<VaultNote>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // VaultLinks configuration
        builder.Entity<VaultLink>(entity =>
        {
            entity.ToTable("VaultLinks");
            
            entity.HasOne(e => e.VaultItem)
                .WithOne(e => e.Link)
                .HasForeignKey<VaultLink>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // VaultCryptoWallets configuration
        builder.Entity<VaultCryptoWallet>(entity =>
        {
            entity.ToTable("VaultCryptoWallets");
            
            entity.HasOne(e => e.VaultItem)
                .WithOne(e => e.CryptoWallet)
                .HasForeignKey<VaultCryptoWallet>(e => e.VaultItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }

    // Add DbSet properties for your entities here
    // Example:
    // public DbSet<User> Users { get; set; }
    // public DbSet<Vault> Vaults { get; set; }
}

