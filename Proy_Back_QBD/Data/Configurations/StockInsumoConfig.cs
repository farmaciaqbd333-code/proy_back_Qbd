using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models.ProductoIntermedio;

public class StockInsumoConfiguration : IEntityTypeConfiguration<StockInsumo>
{
    public void Configure(EntityTypeBuilder<StockInsumo> builder)
    {
        builder.ToTable("stock_insumo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IdCompraInsumo).HasColumnName("id_compra_insumo").IsRequired();
        builder.Property(x => x.IdProductoIntermedio).HasColumnName("id_producto_intermedio").IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").IsRequired();
        builder.Property(x => x.IdCompraInsumo).HasColumnName("id_compra_insumo").IsRequired();
        builder.Property(x => x.StockDisponible).HasColumnName("stock_disponible").HasPrecision(18, 4);
        builder.Property(x => x.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(20);
        builder.Property(x => x.IdSede).HasColumnName("id_sede");
        builder.Property(x => x.IdNotaSalidaInsumo).HasColumnName("id_nota_salida_insumo");

        builder.HasOne(x => x.NotaSalidaInsumo)
            .WithOne(wm => wm.StockInsumo)
            .HasForeignKey<StockInsumo>(x => x.IdNotaSalidaInsumo);

        builder.HasOne(x => x.CompraInsumo)
            .WithMany(wm => wm.StockInsumos)
            .HasForeignKey(x => x.IdCompraInsumo);

        builder.HasOne(x => x.ProductoIntermedio)
            .WithOne(wm => wm.StockInsumo)
            .HasForeignKey<StockInsumo>(x => x.IdProductoIntermedio);

        builder.HasOne(x => x.Sede)
            .WithMany(w => w.StockInsumos)
            .HasForeignKey(x => x.IdSede);
    }
}