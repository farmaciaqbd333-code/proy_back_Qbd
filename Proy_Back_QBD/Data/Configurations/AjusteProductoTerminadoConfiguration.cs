using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AjusteProductoTerminadoConfiguration : IEntityTypeConfiguration<AjusteProductoTerminado>
{
    public void Configure(EntityTypeBuilder<AjusteProductoTerminado> builder)
    {
        builder.ToTable("ajuste_producto_terminado");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
            ;

        builder.Property(x => x.Ajuste)
            .HasColumnName("ajuste")
            .HasColumnType("decimal");

        builder.Property(x => x.IdCompraProducto)
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

        builder.HasOne(x => x.CompraProducto)
            .WithMany(wm => wm.AjusteProductoTerminados)
            .HasForeignKey(x => x.IdCompraProducto)
            ;

        builder.HasOne(x => x.Creador)
            .WithMany()
            .HasForeignKey(x => x.IdCreador)
            .OnDelete(DeleteBehavior.Restrict);
    }
}