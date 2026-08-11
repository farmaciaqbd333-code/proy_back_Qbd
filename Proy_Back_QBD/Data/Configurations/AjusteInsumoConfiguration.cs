using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models;

public class AjusteInsumoConfiguration : IEntityTypeConfiguration<AjusteInsumo>
{
    public void Configure(EntityTypeBuilder<AjusteInsumo> builder)
    {
        builder.ToTable("ajuste_insumo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Ajuste)
            .HasColumnName("ajuste")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.StockAnterior)
            .HasColumnName("stock_anterior")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.StockNuevo)
            .HasColumnName("stock_nuevo")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.IdStockInsumo)
            .HasColumnName("id_compra_insumo")
            .IsRequired();

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .IsRequired();

        builder.Property(x => x.IdCreador)
            .HasColumnName("id_creador")
            .IsRequired();

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion")
            .HasMaxLength(500)
            .IsRequired(false);

        // Relación con CompraInsumos
        builder.HasOne(x => x.StockInsumo)
            .WithMany(wm => wm.AjusteInsumos)
            .HasForeignKey(x => x.IdStockInsumo);

        // Relación con Usuario
        builder.HasOne(x => x.Creador)
            .WithMany(wm => wm.AjusteInsumos)
            .HasForeignKey(x => x.IdCreador);
    }
}