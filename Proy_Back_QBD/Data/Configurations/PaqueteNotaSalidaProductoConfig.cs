using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models;

public class PaqueteNotaSalidaProductoConfiguration : IEntityTypeConfiguration<PaqueteNotaSalidaProducto>
{
    public void Configure(EntityTypeBuilder<PaqueteNotaSalidaProducto> builder)
    {
        builder.ToTable("paquete_nota_salida_producto");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdNotaSalidaProducto)
            .HasColumnName("id_nota_salida_producto");

        builder.Property(e => e.CantidadPaquete)
            .HasColumnName("cantidad_paquete");

        builder.Property(e => e.Peso)
            .HasColumnName("peso");

        builder.Property(e => e.Tara)
            .HasColumnName("tara");

        builder.Property(e => e.Um)
            .HasColumnName("um");

        builder.Property(e => e.PesoNeto)
            .HasColumnName("peso_neto");

        builder.Property(e => e.PesoBruto)
            .HasColumnName("peso_bruto");

        builder.Property(e => e.CantidadPaqueteRecibida)
            .HasColumnName("cantidad_paquete_recibida");

        builder.Property(e => e.PesoRecibida)
            .HasColumnName("peso_recibida");

        builder.Property(e => e.TaraRecibida)
            .HasColumnName("tara_recibida");

        builder.Property(e => e.PesoNetoRecibida)
            .HasColumnName("peso_neto_recibida");

        builder.Property(e => e.PesoBrutoRecibida)
            .HasColumnName("peso_bruto_recibida");

        builder.Property(e => e.IdVerificador)
            .HasColumnName("id_verificador");

        builder.Property(e => e.IdCreador)
            .HasColumnName("id_creador");

        builder.HasOne(e => e.Creador)
            .WithMany()
            .HasForeignKey(e => e.IdCreador)
            .IsRequired(false);

        builder.HasOne(e => e.Verificador)
            .WithMany()
            .HasForeignKey(e => e.IdVerificador)
            .HasConstraintName("fk_paquete_nota_salida_producto_verificador")
            .IsRequired(false);

        builder.HasOne(e => e.NotaSalidaProducto)
            .WithMany(n => n.PaqueteNotaSalidaProductos)
            .HasForeignKey(e => e.IdNotaSalidaProducto)
            .IsRequired(false);
    }
}
