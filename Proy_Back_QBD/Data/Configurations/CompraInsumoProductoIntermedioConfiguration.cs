using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CompraInsumoProductoIntermedioConfiguration
    : IEntityTypeConfiguration<CompraInsumoProductoIntermedio>
{
    public void Configure(EntityTypeBuilder<CompraInsumoProductoIntermedio> builder)
    {
        builder.ToTable("compra_insumo_producto_intermedio");

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

        builder.Property(x => x.IdCompraInsumo)
            .HasColumnName("id_compra_insumo");

        builder.Property(x => x.IdModificador)
            .HasColumnName("id_modificador");

        builder.Property(x => x.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.HasOne(x => x.InsumoProductoIntermedio)
            .WithMany(x => x.CompraInsumoProductoIntermedio)
            .HasForeignKey(x => x.IdInsumoProductoIntermedio);

        builder.HasOne(x => x.CompraInsumo)
            .WithMany(x => x.CompraInsumoProductoIntermedio)
            .HasForeignKey(x => x.IdCompraInsumo);

        builder.HasOne(x => x.Creador)
            .WithMany(w => w.CompraInsumoProductoIntermedioCreados)
            .HasForeignKey(x => x.IdCreador);

        builder.HasOne(x => x.Modificador)
            .WithMany(w => w.CompraInsumoProductoIntermedioModificados)
            .HasForeignKey(x => x.IdModificador);
    }
}