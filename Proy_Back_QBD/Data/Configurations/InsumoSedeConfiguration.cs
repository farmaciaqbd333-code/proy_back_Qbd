
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InsumoSedeConfiguration : IEntityTypeConfiguration<SiteSupply>
{
    public void Configure(EntityTypeBuilder<SiteSupply> builder)
    {
        builder.ToTable("insumo_sedes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdSite)
            .HasColumnName("id_sede");

        builder.Property(x => x.IdSupply)
            .HasColumnName("id_insumo");

        builder.Property(x => x.Location)
            .HasColumnName("ubicacion");

        builder.HasOne(x => x.Sede)
            .WithMany(x => x.SiteSupply)
            .HasForeignKey(x => x.IdSite);

        builder.HasOne(x => x.Insumo)
            .WithMany(x => x.SiteSupply)
            .HasForeignKey(x => x.IdSupply);
    }
}