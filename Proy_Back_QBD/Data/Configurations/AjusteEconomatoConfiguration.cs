using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AjusteEconomatoConfiguration : IEntityTypeConfiguration<AjusteEconomato>
{
    public void Configure(EntityTypeBuilder<AjusteEconomato> builder)
    {
        builder.ToTable("ajuste_economato");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Ajuste)
            .HasColumnName("ajuste")
            .HasColumnType("numeric");

        builder.Property(x => x.IdCompraEconomato)
            .HasColumnName("id_compra_economato");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.FechaModificacion)
            .HasColumnName("fecha_modificacion")
            .HasColumnType("timestamptz");

        builder.Property(x => x.IdCreador)
            .HasColumnName("id_creador");

        builder.Property(x => x.IdModificador)
            .HasColumnName("id_modificador");

        builder.Property(x => x.StockAnterior)
            .HasColumnName("stock_anterior")
            .HasColumnType("numeric");

        builder.Property(x => x.StockNuevo)
            .HasColumnName("stock_nuevo")
            .HasColumnType("numeric");

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion");

        builder.HasOne(x => x.CompraEconomato)
            .WithMany(wm => wm.AjusteEconomatos)
            .HasForeignKey(x => x.IdCompraEconomato);

        builder.HasOne(x => x.Creador)
            .WithMany()
            .HasForeignKey(x => x.IdCreador);
    }
}