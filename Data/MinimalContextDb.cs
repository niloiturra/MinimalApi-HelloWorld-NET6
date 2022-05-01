using Microsoft.EntityFrameworkCore;
using MinimalApiDemo.Entities;

namespace MinimalApiDemo.Data
{
    public class MinimalContextDb : DbContext
    {
        public MinimalContextDb(DbContextOptions<MinimalContextDb> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .IsRequired()
                .HasColumnType("varchar(80)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Description)
                .IsRequired()
                .HasColumnType("varchar(250)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .IsRequired();

            modelBuilder.Entity<Product>()
                .Property(p => p.Amount)
                .IsRequired();

            modelBuilder.Entity<Product>()
                .Property(p => p.Active)
                .IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}
