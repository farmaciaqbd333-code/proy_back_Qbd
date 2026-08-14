using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models;

public class NotaSalidaEconomatoConfiguration : IEntityTypeConfiguration<NotaSalidaEconomato>
{
    public void Configure(EntityTypeBuilder<NotaSalidaEconomato> builder)
    {
        builder.ToTable("nota_salida_economato");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdNotaSalida)
            .HasColumnName("id_nota_salida");

        builder.Property(e => e.IdCompraEconomato)
            .HasColumnName("id_compra_economato");

        builder.Property(e => e.Cantidad)
            .HasColumnName("cantidad");

        builder.Property(e => e.Um)
            .HasColumnName("um");

        builder.Property(e => e.Lote)
            .HasColumnName("lote");

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

        builder.Property(e => e.CantidadRecibida)
.HasColumnName("cantidad_recibida");

        builder.HasOne(e => e.NotaSalida)
            .WithMany(wm => wm.NotaSalidaEconomatos)
            .HasForeignKey(e => e.IdNotaSalida)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CompraEconomato)
            .WithMany(wm => wm.NotaSalidaEconomatos)
            .HasForeignKey(e => e.IdCompraEconomato)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Creador)
            .WithMany(wm => wm.NotaSalidaEconomatoCreados)
            .HasForeignKey(e => e.IdCreador)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Modificador)
            .WithMany(wm => wm.NotaSalidaEconomatoModificados)
            .HasForeignKey(e => e.IdModificador)
            .OnDelete(DeleteBehavior.Cascade);
    }
}