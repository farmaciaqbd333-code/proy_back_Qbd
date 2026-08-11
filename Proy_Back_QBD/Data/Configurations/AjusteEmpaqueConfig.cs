using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models;

public class AjusteEmpaqueConfiguration : IEntityTypeConfiguration<AjusteEmpaque>
{
    public void Configure(EntityTypeBuilder<AjusteEmpaque> builder)
    {
        builder.ToTable("ajuste_empaque");

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

        builder.Property(x => x.IdStockEmpaque)
            .HasColumnName("id_stock_empaque")
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

        builder.HasOne(x => x.StockEmpaque)
            .WithMany(wm => wm.AjusteEmpaques)
            .HasForeignKey(x => x.IdStockEmpaque);

        builder.HasOne(x => x.Creador)
            .WithMany(wm => wm.AjusteEmpaques)
            .HasForeignKey(x => x.IdCreador);
    }
}