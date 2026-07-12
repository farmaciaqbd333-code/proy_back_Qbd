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

            builder.Property(x => x.Tipo)
                .HasColumnName("tipo");

            builder.Property(x => x.Cantidad)
                .HasColumnName("cantidad");

            builder.Property(x => x.Um)
                .HasColumnName("um");

            builder.Property(x => x.FechaEmision)
                .HasColumnName("fecha_emision");

            builder.Property(x => x.FechaVencimiento)
                .HasColumnName("fecha_vencimiento");

            builder.Property(x => x.Elaborado)
                .HasColumnName("elaborado");

            builder.Property(x => x.Autorizado)
                .HasColumnName("autorizado");

            builder.Property(x => x.Procedimiento)
                .HasColumnName("procedimiento");

            builder.Property(x => x.IdEmpaque)
                .HasColumnName("id_empaque");

            builder.Property(x => x.IdEtiqueta)
                .HasColumnName("id_etiqueta");

            builder.Property(x => x.CodTermo)
                .HasColumnName("cod_termo");

            builder.Property(x => x.CodEtiqueta1)
                .HasColumnName("cod_etiqueta1");

            builder.Property(x => x.CodEtiqueta2)
                .HasColumnName("cod_etiqueta2");

            builder.Property(x => x.CodAdicional)
                .HasColumnName("cod_adicional");

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
                .HasColumnName("fecha_modificacion")
                .ValueGeneratedOnAddOrUpdate();

            builder.Property(x => x.IdCreador)
                .HasColumnName("id_creador");

            builder.Property(x => x.IdModificador)
                .HasColumnName("id_modificador");

            // Relaciones

            builder.HasOne(x => x.Insumo)
            .WithMany()
            .HasForeignKey(x => x.IdInsumo)
            ;

            builder.HasOne(x => x.Empaque)
            .WithMany()
            .HasForeignKey(x => x.IdEmpaque);

            builder.HasOne(x => x.Creador)
                .WithMany(wm => wm.ProductosIntermediosCreados)
                .HasForeignKey(x => x.IdCreador);

            builder.HasOne(x => x.Modificador)
                .WithMany(wm => wm.ProductosIntermediosModificados)
                .HasForeignKey(x => x.IdModificador);
        }
    }
}