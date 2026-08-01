//Es prácticamente el PrismaClient.

using Domain.Entities; // "import" de las entidades 
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

// Este archivo es el cliente Prisma. Aquí se conectan todas las tablas.
public class AppDbContext : DbContext
{
    // Constructor: recibe la configuración (qué base de datos usar) y se la pasa al padre
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Cada DbSet<T> = una tabla. Esto es literalmente tu "prisma.usuario", "prisma.tenant", etc.
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    // Aquí defines reglas extra que no van en la clase misma (como @unique en Prisma)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();   // como email String @unique en tu schema.prisma

        modelBuilder.Entity<Solicitud>()
        .HasIndex(s => new { s.TenantId, s.Codigo })
        .IsUnique();
        
        modelBuilder.Entity<Solicitud>()
    .HasOne(s => s.Categoria)
    .WithMany()
    .HasForeignKey(s => s.CategoriaId);
    }

    
}