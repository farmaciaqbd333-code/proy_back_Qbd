using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StockInsumoProductoIntermedioConfiguration
    : IEntityTypeConfiguration<StockInsumoProductoIntermedio>
{
    public void Configure(EntityTypeBuilder<StockInsumoProductoIntermedio> builder)
    {
        builder.ToTable("stock_insumo_producto_intermedio");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.IdInsumoProductoIntermedio)
            .HasColumnName("id_insumo_producto_intermedio");

        builder.Property(x => x.Cantidad)
            .HasColumnName("cantidad");

        builder.Property(x => x.UnidadMedida)
            .HasColumnName("unidad_medida");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(x => x.IdCreador)
            .HasColumnName("id_creador");

        builder.Property(x => x.IdStockInsumo)
            .HasColumnName("id_stock_insumo");

        builder.Property(x => x.IdModificador)
            .HasColumnName("id_modificador");

        builder.Property(x => x.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.HasOne(x => x.InsumoProductoIntermedio)
            .WithMany(x => x.StockInsumoProductoIntermedios)
            .HasForeignKey(x => x.IdInsumoProductoIntermedio);

        builder.HasOne(x => x.StockInsumo)
            .WithMany(x => x.StockInsumoProductoIntermedio)
            .HasForeignKey(x => x.IdStockInsumo);
    }
}