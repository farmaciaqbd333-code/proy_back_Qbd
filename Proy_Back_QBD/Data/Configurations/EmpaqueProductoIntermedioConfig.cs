using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EmpaqueProductoIntermedioConfig : IEntityTypeConfiguration<EmpaqueProductoIntermedio>
{
    public void Configure(EntityTypeBuilder<EmpaqueProductoIntermedio> builder)
    {
        builder.ToTable("empaque_producto_intermedio");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdEmpaque)
            .HasColumnName("id_empaque")
            .IsRequired();

        builder.Property(x => x.IdProductoIntermedio)
            .HasColumnName("id_producto_intermedio")
            .IsRequired();

        builder.HasOne(x => x.Empaque)
            .WithMany(x => x.EmpaqueProductoIntermedios)
            .HasForeignKey(x => x.IdEmpaque);

        builder.HasOne(x => x.ProductoIntermedio)
            .WithMany(x => x.EmpaqueProductoIntermedios)
            .HasForeignKey(x => x.IdProductoIntermedio);
    }
}