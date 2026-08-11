using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StockEmpaqueProductoIntermedioConfig : IEntityTypeConfiguration<StockEmpaqueProductoIntermedio>
{
    public void Configure(EntityTypeBuilder<StockEmpaqueProductoIntermedio> builder)
    {
        builder.ToTable("stock_empaque_producto_intermedio");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        ;

        builder.Property(x => x.IdStockEmpaque)
            .HasColumnName("id_stock_empaque")
            .IsRequired();

        builder.Property(x => x.IdEmpaqueProductoIntermedio)
            .HasColumnName("id_empaque_producto_intermedio")
            .IsRequired();

        builder.Property(x => x.Cantidad)
            .HasColumnName("cantidad")
            .IsRequired();

        builder.Property(x => x.UnidadMedida)
            .HasColumnName("unidad_medida")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(x => x.StockEmpaque)
            .WithMany(x => x.StockEmpaqueProductoIntermedio)
            .HasForeignKey(x => x.IdStockEmpaque)
            .HasPrincipalKey(x => x.Id);

        builder.HasOne(x => x.EmpaqueProductoIntermedio)
            .WithMany(x => x.StockEmpaqueProductoIntermedios)
            .HasForeignKey(x => x.IdEmpaqueProductoIntermedio)
            .HasPrincipalKey(x => x.Id);
    }
}