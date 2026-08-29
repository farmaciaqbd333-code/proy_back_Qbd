
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InsumoSedeConfiguration : IEntityTypeConfiguration<InsumoSede>
{
    public void Configure(EntityTypeBuilder<InsumoSede> builder)
    {
        builder.ToTable("insumo_sedes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdSede)
            .HasColumnName("id_sede");

        builder.Property(x => x.IdInsumo)
            .HasColumnName("id_insumo");

        builder.Property(x => x.Ubicacion)
            .HasColumnName("ubicacion");

        builder.HasOne(x => x.sede)
            .WithMany(x => x.InsumoSedes)
            .HasForeignKey(x => x.IdSede);

        builder.HasOne(x => x.insumo)
            .WithMany(x => x.InsumoSedes)
            .HasForeignKey(x => x.IdInsumo);
    }
}