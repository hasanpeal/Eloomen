using Microsoft.EntityFrameworkCore;

namespace server.Models;

public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        : base(options)
    {
    }

    // Add DbSet properties for your entities here
    // Example:
    // public DbSet<User> Users { get; set; }
    // public DbSet<Vault> Vaults { get; set; }
}

