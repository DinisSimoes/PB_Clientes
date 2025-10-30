using Microsoft.EntityFrameworkCore;
using PB_Clientes.Domain.Entities;

namespace PB_Clientes.Infrastructure.Data
{
    public class ClientesDbContext : DbContext
    {
        public ClientesDbContext(DbContextOptions<ClientesDbContext> options) : base(options) { }
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Nome).IsRequired().HasMaxLength(50);
                b.Property(x => x.Cpf).IsRequired().HasMaxLength(14);
                b.Property(x => x.DataCriacao).IsRequired();
            });

            modelBuilder.Entity<OutboxMessage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.MessageType).IsRequired().HasMaxLength(200);
                b.Property(x => x.Payload).IsRequired();
                b.Property(x => x.OccurredUtc).IsRequired();
                b.Property(x => x.Status).IsRequired().HasMaxLength(50);
                b.Property(x => x.Attempts).IsRequired();
                b.Property(x => x.LastError).HasMaxLength(1000);
            });
        }
    }
}
