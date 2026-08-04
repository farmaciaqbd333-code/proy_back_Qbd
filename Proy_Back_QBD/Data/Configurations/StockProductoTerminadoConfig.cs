using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StockProductoTerminadoConfiguration : IEntityTypeConfiguration<StockProducto>
{
    public void Configure(EntityTypeBuilder<StockProducto> builder)
    {
        builder.ToTable("stock_producto_terminado");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IdCompraProducto).HasColumnName("id_compra_producto").IsRequired();
        builder.Property(x => x.StockDisponible).HasColumnName("stock_disponible").HasPrecision(18, 4);
        builder.Property(x => x.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(20);
        builder.Property(x => x.IdSede).HasColumnName("id_sede");
        builder.Property(x => x.IdNotaSalidaProducto).HasColumnName("id_nota_salida_producto");

        builder.HasOne(x => x.NotaSalidaProducto)
            .WithOne(wm => wm.StockProductoTerminado)
            .HasForeignKey<StockProducto>(x => x.IdNotaSalidaProducto);
            
        builder.HasOne(x => x.CompraProducto)
            .WithMany(w => w.StockProductoTerminados)
            .HasForeignKey(x => x.IdCompraProducto);

        builder.HasOne(x => x.Sede)
            .WithMany(w => w.StockProductoTerminados)
            .HasForeignKey(x => x.IdSede);
    }
}