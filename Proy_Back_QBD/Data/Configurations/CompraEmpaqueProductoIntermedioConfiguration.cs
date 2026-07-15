using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CompraEmpaqueProductoIntermedioConfig : IEntityTypeConfiguration<CompraEmpaqueProductoIntermedio>
{
    public void Configure(EntityTypeBuilder<CompraEmpaqueProductoIntermedio> builder)
    {
        builder.ToTable("compra_empaque_producto_intermedio");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdCompraEmpaque)
            .HasColumnName("id_compra_empaque")
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

        builder.HasOne(x => x.CompraEmpaque)
            .WithMany(x => x.CompraEmpaqueProductoIntermedios)
            .HasForeignKey(x => x.IdCompraEmpaque);

        builder.HasOne(x => x.EmpaqueProductoIntermedio)
            .WithMany(x => x.CompraEmpaqueProductoIntermedios)
            .HasForeignKey(x => x.IdEmpaqueProductoIntermedio);
    }
}