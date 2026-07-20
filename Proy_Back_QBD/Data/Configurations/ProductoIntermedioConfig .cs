using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using proy_back_Qbd.Models.ProductoIntermedio;

namespace proy_back_Qbd.Data.Configurations
{
    public class ProductoIntermedioConfiguration : IEntityTypeConfiguration<ProductoIntermedio>
    {
        public void Configure(EntityTypeBuilder<ProductoIntermedio> builder)
        {
            builder.ToTable("producto_intermedio");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Lote)
                .HasColumnName("lote");

            builder.Property(x => x.IdInsumo)
                .HasColumnName("id_insumo");

            builder.Property(x => x.LoteEstandar)
                .HasColumnName("lote_estandar");

            builder.Property(x => x.PesoUnidad)
                .HasColumnName("peso_unidad");

            builder.Property(x => x.LoteEstTotal)
                .HasColumnName("lote_est_total");

            builder.Property(x => x.Tipo)
                .HasColumnName("tipo");

            builder.Property(x => x.TipoUso)
                .HasColumnName("tipo_uso");

            builder.Property(x => x.Cantidad)
                .HasColumnName("cantidad");

            builder.Property(x => x.Um)
                .HasColumnName("um");

            builder.Property(x => x.FechaEmision)
                .HasColumnName("fecha_emision");

            builder.Property(x => x.FechaVencimiento)
                .HasColumnName("fecha_vencimiento");

            builder.Property(x => x.IdElaborado)
                .HasColumnName("id_elaborado");

            builder.Property(x => x.IdAutorizado)
                .HasColumnName("id_autorizado");

            builder.Property(x => x.Procedimiento)
                .HasColumnName("procedimiento");



            builder.Property(x => x.Aspecto)
                .HasColumnName("aspecto");

            builder.Property(x => x.Color)
                .HasColumnName("color");

            builder.Property(x => x.Olor)
                .HasColumnName("olor");

            builder.Property(x => x.Ph)
                .HasColumnName("ph");

            builder.Property(x => x.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .ValueGeneratedOnAddOrUpdate();

            builder.Property(x => x.FechaModificacion)
                .HasColumnName("fecha_modificacion");

            builder.Property(x => x.IdCreador)
                .HasColumnName("id_creador");

            builder.Property(x => x.IdModificador)
                .HasColumnName("id_modificador");

            // Relaciones

            builder.HasOne(x => x.Insumo)
            .WithMany(x => x.ProductoIntermedio)
            .HasForeignKey(x => x.IdInsumo)
            ;

            builder.HasOne(x => x.Creador)
                .WithMany(wm => wm.ProductosIntermediosCreados)
                .HasForeignKey(x => x.IdCreador);
                
            builder.HasOne(x => x.Elaborador)
                .WithMany(wm => wm.ProductosIntermediosElaborados)
                .HasForeignKey(x => x.IdElaborado);

            builder.HasOne(x => x.Autorizador)
                .WithMany(wm => wm.ProductosIntermediosAutorizados)
                .HasForeignKey(x => x.IdAutorizado);

            builder.HasOne(x => x.Modificador)
                .WithMany(wm => wm.ProductosIntermediosModificados)
                .HasForeignKey(x => x.IdModificador);
        }
    }
}