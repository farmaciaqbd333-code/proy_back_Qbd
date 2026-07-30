using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StockEconomatoConfiguration : IEntityTypeConfiguration<StockEconomato>
{
    public void Configure(EntityTypeBuilder<StockEconomato> builder)
    {
        builder.ToTable("stock_economato");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IdCompraEconomato).HasColumnName("id_compra_economato").IsRequired();
        builder.Property(x => x.StockDisponible).HasColumnName("stock_disponible").HasPrecision(18, 4);
        builder.Property(x => x.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(20);
        builder.Property(x => x.IdSede).HasColumnName("id_sede");
        builder.Property(x => x.IdNotaSalidaEconomato).HasColumnName("id_nota_salida_economato");

        builder.HasOne(x => x.NotaSalidaEconomato)
            .WithOne(wm => wm.StockEconomato)
            .HasForeignKey<StockEconomato>(x => x.IdCompraEconomato);
        builder.HasOne(x => x.CompraEconomato)
            .WithMany(w => w.StockEconomatos)
            .HasForeignKey(x => x.IdCompraEconomato);

        builder.HasOne(x => x.Sede)
            .WithMany(w => w.StockEconomatos)
            .HasForeignKey(x => x.IdSede);
    }
}