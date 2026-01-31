using Microsoft.EntityFrameworkCore;
using SistemaVotoModelos;

namespace SistemaVotoAPI.Data
{
    public class APIVotosDbContext : DbContext
    {
        public APIVotosDbContext(DbContextOptions<APIVotosDbContext> options)
            : base(options)
        {
        }

        public DbSet<Votante> Votantes { get; set; } = default!;
        public DbSet<Candidato> Candidatos { get; set; } = default!;
        public DbSet<Eleccion> Elecciones { get; set; } = default!;
        public DbSet<Lista> Listas { get; set; } = default!;
        public DbSet<VotoAnonimo> VotosAnonimos { get; set; } = default!;
        public DbSet<TokenAcceso> TokensAcceso { get; set; } = default!;
        public DbSet<Junta> Juntas { get; set; } = default!;
        public DbSet<Direccion> Direcciones { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= DIRECCION =================
            modelBuilder.Entity<Direccion>()
                .HasKey(d => d.Id);

            // ================= ELECCION =================
            modelBuilder.Entity<Eleccion>()
                .HasKey(e => e.Id);

            // ================= LISTA =================
            modelBuilder.Entity<Lista>()
                .HasKey(l => l.Id);

            modelBuilder.Entity<Lista>()
                .HasOne(l => l.Eleccion)
                .WithMany(e => e.Listas)
                .HasForeignKey(l => l.EleccionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ================= JUNTA =================
            modelBuilder.Entity<Junta>(entity =>
            {
                entity.HasKey(j => j.Id);
                entity.Property(j => j.Id).ValueGeneratedOnAdd();

                entity.HasOne(j => j.Direccion)
                    .WithMany(d => d.Juntas)
                    .HasForeignKey(j => j.DireccionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(j => j.JefeDeJunta)
                    .WithMany()
                    .HasForeignKey(j => j.JefeDeJuntaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(j => j.Eleccion)
                    .WithMany(e => e.Juntas)
                    .HasForeignKey(j => j.EleccionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ================= VOTANTE =================
            modelBuilder.Entity<Votante>()
                .HasKey(v => v.Cedula);

            modelBuilder.Entity<Votante>()
                .HasOne(v => v.Junta)
                .WithMany(j => j.Votantes)
                .HasForeignKey(v => v.JuntaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================= CANDIDATO =================
            modelBuilder.Entity<Candidato>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.Votante)
                .WithMany()
                .HasForeignKey(c => c.Cedula)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.Eleccion)
                .WithMany(e => e.Candidatos)
                .HasForeignKey(c => c.EleccionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.Lista)
                .WithMany(l => l.Candidatos)
                .HasForeignKey(c => c.ListaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Candidato>()
                .HasIndex(c => new { c.Cedula, c.EleccionId })
                .IsUnique();

            // ================= VOTO ANONIMO =================
            modelBuilder.Entity<VotoAnonimo>()
                .HasKey(v => v.Id);

            modelBuilder.Entity<VotoAnonimo>()
                .HasOne(v => v.Eleccion)
                .WithMany(e => e.VotosAnonimos)
                .HasForeignKey(v => v.EleccionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VotoAnonimo>()
                .HasOne(v => v.Direccion)
                .WithMany(d => d.VotosAnonimos)
                .HasForeignKey(v => v.DireccionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VotoAnonimo>()
                .HasOne(v => v.Lista)
                .WithMany(l => l.VotosAnonimos)
                .HasForeignKey(v => v.ListaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ================= TOKEN ACCESO =================
            modelBuilder.Entity<TokenAcceso>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TokenAcceso>()
                .HasOne(t => t.Votante)
                .WithMany(v => v.TokensAcceso)
                .HasForeignKey(t => t.VotanteId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
