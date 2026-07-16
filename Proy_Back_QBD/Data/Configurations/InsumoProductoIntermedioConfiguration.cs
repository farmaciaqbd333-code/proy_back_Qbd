using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InsumoProductoIntermedioConfiguration : IEntityTypeConfiguration<InsumoProductoIntermedio>
{
    public void Configure(EntityTypeBuilder<InsumoProductoIntermedio> builder)
    {
        builder.ToTable("insumo_producto_intermedio");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdInsumo)
            .HasColumnName("id_insumo");

        builder.Property(x => x.Porcentaje)
            .HasColumnName("porcentaje");

        builder.Property(x => x.Variable)
            .HasColumnName("v");

        builder.Property(x => x.CantidadUnidad)
            .HasColumnName("cantidad_unidad");

        builder.Property(x => x.FactorCorrecion)
            .HasColumnName("factor_correcion");

        builder.Property(x => x.Dilucion)
            .HasColumnName("dilucion");

        builder.Property(x => x.UnidadMedida)
            .HasColumnName("unidad_medida");

        builder.Property(x => x.CantidadLote)
            .HasColumnName("cantidad_lote");

        builder.Property(x => x.Practica)
            .HasColumnName("practica");

        builder.Property(x => x.Csp)
            .HasColumnName("csp");

        builder.Property(x => x.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        ;

        builder.Property(x => x.FechaModificacion)
            .HasColumnName("fecha_modificacion");

        builder.Property(x => x.IdProductoIntermedio)
            .HasColumnName("id_producto_intermedio");

        builder.Property(x => x.IdCreador)
            .HasColumnName("id_creador");

        builder.Property(x => x.IdModificador)
            .HasColumnName("id_modificador");

        builder.HasOne(x => x.Insumo)
            .WithMany(wm => wm.InsumoProductoIntermedio)
            .HasForeignKey(x => x.IdInsumo);

        builder.HasOne(x => x.ProductoIntermedio)
            .WithMany(wm => wm.InsumoProductoIntermedio)
            .HasForeignKey(x => x.IdProductoIntermedio);

        builder.HasOne(x => x.Creador)
            .WithMany(wm => wm.InsumoProductoIntermedioCreados)
            .HasForeignKey(hfk => hfk.IdCreador);

        builder.HasOne(x => x.Modificador)
            .WithMany(wm => wm.InsumoProductoIntermedioModificados)
            .HasForeignKey(hfk => hfk.IdModificador);

        builder.Navigation(x => x.CompraInsumoProductoIntermedio);
    }
}