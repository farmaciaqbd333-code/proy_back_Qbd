using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proy_back_QBD.Models;

public class EconomatoSedeConfiguration : IEntityTypeConfiguration<EconomatoSede>
{
    public void Configure(EntityTypeBuilder<EconomatoSede> builder)
    {
        builder.ToTable("economato_sedes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdSite)
            .HasColumnName("id_sede");

        builder.Property(x => x.IdEconomato)
            .HasColumnName("id_economato");

        builder.Property(x => x.Location)
            .HasColumnName("ubicacion");

        builder.Property(x => x.Limite)
            .HasColumnName("limite");

        builder.HasOne(x => x.Sede)
            .WithMany(x => x.EconomatoSedes)
            .HasForeignKey(x => x.IdSite);

        builder.HasOne(x => x.Economato)
            .WithMany(x => x.EconomatoSedes)
            .HasForeignKey(x => x.IdEconomato);
    }
}
