using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models;

public class NotaSalidaInsumoConfiguration : IEntityTypeConfiguration<NotaSalidaInsumo>
{
    public void Configure(EntityTypeBuilder<NotaSalidaInsumo> builder)
    {
        builder.ToTable("nota_salida_insumo");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdInsumo)
            .HasColumnName("id_insumo");

        builder.Property(e => e.Cantidad)
            .HasColumnName("cantidad");

        builder.Property(e => e.Um)
            .HasColumnName("um");

        builder.Property(e => e.Lote)
            .HasColumnName("lote");

        builder.Property(e => e.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(e => e.FechaModificacion)
            .HasColumnName("fecha_modificacion")
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(e => e.IdCreador)
            .HasColumnName("id_creador");

        builder.Property(e => e.IdModificador)
            .HasColumnName("id_modificador");

        builder.Property(e => e.Tara)
            .HasColumnName("tara");

        builder.Property(e => e.PesoBruto)
            .HasColumnName("peso_bruto");

        builder.Property(e => e.PesoNeto)
            .HasColumnName("peso_neto");

        builder.Property(e => e.Paquete)
            .HasColumnName("paquete");

        builder.Property(e => e.CantidadPaquete)
            .HasColumnName("cantidad_paquete");

        builder.HasOne(e => e.Insumo)
            .WithMany(wm => wm.NotaSalidaInsumos)
            .HasForeignKey(e => e.IdInsumo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Creador)
            .WithMany(wm => wm.NotaSalidaInsumoCreadas)
            .HasForeignKey(e => e.IdCreador)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Modificador)
            .WithMany(wm => wm.NotaSalidaInsumoModificadas)
            .HasForeignKey(e => e.IdModificador)
            .OnDelete(DeleteBehavior.Restrict);
    }
}