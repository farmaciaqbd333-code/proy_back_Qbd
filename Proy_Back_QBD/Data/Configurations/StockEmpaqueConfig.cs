using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StockEmpaqueConfiguration : IEntityTypeConfiguration<StockEmpaque>
{
    public void Configure(EntityTypeBuilder<StockEmpaque> builder)
    {
        builder.ToTable("stock_empaque");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IdCompraEmpaque).HasColumnName("id_compra_empaque").IsRequired();
        builder.Property(x => x.StockDisponible).HasColumnName("stock_disponible").HasPrecision(18, 4);
        builder.Property(x => x.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(20);
        builder.Property(x => x.IdSede).HasColumnName("id_sede");
        builder.Property(x => x.IdNotaSalidaEmpaque).HasColumnName("id_nota_salida_empaque");

        builder.HasOne(x => x.NotaSalidaEmpaque)
            .WithOne(wm => wm.StockEmpaque)
            .HasForeignKey<StockEmpaque>(x => x.IdNotaSalidaEmpaque);

        builder.HasOne(x => x.CompraEmpaque)
            .WithMany(w => w.StockEmpaques)
            .HasForeignKey(x => x.IdCompraEmpaque);

        builder.HasOne(x => x.Sede)
            .WithMany(w => w.StockEmpaques)
            .HasForeignKey(x => x.IdSede);
    }
}