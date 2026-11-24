using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proy_back_QBD.Models.AppMedico;

namespace Proy_back_QBD.Data.Configurations
{
    // Tablas de la app de médicos. La estructura real la crea DatabaseStartupMigrator (SQL/app_medico.sql).

    public class MedicoAccesoConfig : IEntityTypeConfiguration<MedicoAcceso>
    {
        public void Configure(EntityTypeBuilder<MedicoAcceso> b)
        {
            b.HasIndex(a => a.Cmp).IsUnique();
        }
    }

    public class MedicoProductoFavoritoConfig : IEntityTypeConfiguration<MedicoProductoFavorito>
    {
        public void Configure(EntityTypeBuilder<MedicoProductoFavorito> b)
        {
            b.HasIndex(f => new { f.Cmp, f.ListaPrecioId }).IsUnique();
        }
    }

    public class PedidoMedicoConfig : IEntityTypeConfiguration<PedidoMedico>
    {
        public void Configure(EntityTypeBuilder<PedidoMedico> b)
        {
            b.Property(p => p.Total).HasPrecision(12, 2);
            b.HasOne(p => p.Medico).WithMany().HasForeignKey(p => p.MedicoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.Sede).WithMany().HasForeignKey(p => p.SedeId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(p => p.Detalles).WithOne(d => d.PedidoMedico!).HasForeignKey(d => d.PedidoMedicoId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(p => new { p.Cmp, p.FechaCreacion });
            b.HasIndex(p => new { p.SedeId, p.Estado });
        }
    }

    public class PedidoMedicoDetalleConfig : IEntityTypeConfiguration<PedidoMedicoDetalle>
    {
        public void Configure(EntityTypeBuilder<PedidoMedicoDetalle> b)
        {
            b.Property(d => d.PrecioUnitario).HasPrecision(10, 2);
            b.Property(d => d.Subtotal).HasPrecision(12, 2);
        }
    }
}
