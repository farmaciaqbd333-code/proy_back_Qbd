using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proy_back_QBD.Models;

public class ProductoSedeConfiguration : IEntityTypeConfiguration<ProductoSede>
{
    public void Configure(EntityTypeBuilder<ProductoSede> builder)
    {
        builder.ToTable("producto_sedes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdSite)
            .HasColumnName("id_sede");

        builder.Property(x => x.IdProducto)
            .HasColumnName("id_producto");

        builder.Property(x => x.Location)
            .HasColumnName("ubicacion");

        builder.Property(x => x.Limite)
            .HasColumnName("limite");

        builder.HasOne(x => x.Sede)
            .WithMany(x => x.ProductoSedes)
            .HasForeignKey(x => x.IdSite);

        builder.HasOne(x => x.Producto)
            .WithMany(x => x.ProductoSedes)
            .HasForeignKey(x => x.IdProducto);
    }
}
