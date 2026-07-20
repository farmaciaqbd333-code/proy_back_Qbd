using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models;

public class NotaSalidaConfiguration : IEntityTypeConfiguration<NotaSalida>
{
    public void Configure(EntityTypeBuilder<NotaSalida> builder)
    {
        builder.ToTable("nota_salida");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.FechaSalida)
            .HasColumnName("fecha_salida");

        builder.Property(e => e.Destino)
            .HasColumnName("destino")
            .IsRequired();

        builder.Property(e => e.Observacion)
            .HasColumnName("observacion");

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

        builder.HasOne(e => e.Creador)
            .WithMany()
            .HasForeignKey(e => e.IdCreador)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Modificador)
            .WithMany()
            .HasForeignKey(e => e.IdModificador)
            .OnDelete(DeleteBehavior.Restrict);
    }
}