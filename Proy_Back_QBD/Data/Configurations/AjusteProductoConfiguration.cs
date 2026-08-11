using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AjusteProductoTerminadoConfiguration : IEntityTypeConfiguration<AjusteProducto>
{
    public void Configure(EntityTypeBuilder<AjusteProducto> builder)
    {
        builder.ToTable("ajuste_producto");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
            ;

        builder.Property(x => x.Ajuste)
            .HasColumnName("ajuste")
            .HasColumnType("decimal");

        builder.Property(x => x.IdStockProducto)
            .HasColumnName("id_compra_producto");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(x => x.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(x => x.IdCreador)
            .HasColumnName("id_creador");

        builder.Property(x => x.IdModificador)
            .HasColumnName("id_modificador");

        builder.Property(x => x.StockAnterior)
            .HasColumnName("stock_anterior")
            .HasColumnType("decimal");

        builder.Property(x => x.StockNuevo)
            .HasColumnName("stock_nuevo")
            .HasColumnType("decimal");

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion");

        builder.HasOne(x => x.StockProducto)
            .WithMany(wm => wm.AjusteProductos)
            .HasForeignKey(x => x.IdStockProducto);

        builder.HasOne(x => x.Creador)
            .WithMany(wm => wm.AjusteProductos)
            .HasForeignKey(x => x.IdCreador);
    }
}