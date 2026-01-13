using Demo.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Demo.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Klant> Klanten { get; set; }
    public DbSet<Boek> Boeken { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderRegel> OrderRegels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.OrderRegels)
            .WithOne()
            .HasForeignKey(or => or.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Boek>()
            .Property(b => b.Prijs)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.TotaalBedrag)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrderRegel>()
            .Property(or => or.Prijs)
            .HasColumnType("decimal(18,2)");

        // ?? Note: Klant seed data is now handled in Program.cs with encryption
        // Seed data is added at runtime with encrypted PII fields

        // Seed data: 10 boeken met voorraad
        modelBuilder.Entity<Boek>().HasData(
            new Boek { Id = 1, Titel = "C# in Depth", Auteur = "Jon Skeet", Prijs = 49.99m, VoorraadAantal = 25, ISBN = "978-1617294532" },
            new Boek { Id = 2, Titel = "Clean Code", Auteur = "Robert C. Martin", Prijs = 39.99m, VoorraadAantal = 30, ISBN = "978-0132350884" },
            new Boek { Id = 3, Titel = "The Pragmatic Programmer", Auteur = "Andrew Hunt", Prijs = 44.99m, VoorraadAantal = 20, ISBN = "978-0135957059" },
            new Boek { Id = 4, Titel = "Design Patterns", Auteur = "Gang of Four", Prijs = 54.99m, VoorraadAantal = 15, ISBN = "978-0201633610" },
            new Boek { Id = 5, Titel = "Refactoring", Auteur = "Martin Fowler", Prijs = 42.99m, VoorraadAantal = 18, ISBN = "978-0134757599" },
            new Boek { Id = 6, Titel = "Head First Design Patterns", Auteur = "Eric Freeman", Prijs = 37.99m, VoorraadAantal = 22, ISBN = "978-0596007126" },
            new Boek { Id = 7, Titel = "Code Complete", Auteur = "Steve McConnell", Prijs = 52.99m, VoorraadAantal = 12, ISBN = "978-0735619678" },
            new Boek { Id = 8, Titel = "The Clean Coder", Auteur = "Robert C. Martin", Prijs = 34.99m, VoorraadAantal = 28, ISBN = "978-0137081073" },
            new Boek { Id = 9, Titel = "Working Effectively with Legacy Code", Auteur = "Michael Feathers", Prijs = 46.99m, VoorraadAantal = 16, ISBN = "978-0131177055" },
            new Boek { Id = 10, Titel = "Domain-Driven Design", Auteur = "Eric Evans", Prijs = 58.99m, VoorraadAantal = 10, ISBN = "978-0321125217" }
        );
    }
}
