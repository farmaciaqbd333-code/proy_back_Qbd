using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models;

public class NotaSalidaInsumoConfiguration : IEntityTypeConfiguration<NotaSalidaFamilias>
{
    public void Configure(EntityTypeBuilder<NotaSalidaFamilias> builder)
    {
        builder.ToTable("nota_salida_familias");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdNotaSalida)
            .HasColumnName("id_nota_salida");

        builder.Property(e => e.IdFamilia)
            .HasColumnName("id_familia");

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

        builder.HasOne(e => e.Familia)
            .WithMany(wm => wm.NotaSalidaFamilias)
            .HasForeignKey(e => e.IdFamilia)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.NotaSalida)
            .WithMany(wm => wm.NotaSalidaFamilias)
            .HasForeignKey(e => e.IdNotaSalida)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Creador)
            .WithMany(wm => wm.NotaSalidaInsumoCreadas)
            .HasForeignKey(e => e.IdCreador)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Modificador)
            .WithMany(wm => wm.NotaSalidaInsumoModificadas)
            .HasForeignKey(e => e.IdModificador)
            .OnDelete(DeleteBehavior.Cascade);
    }
}