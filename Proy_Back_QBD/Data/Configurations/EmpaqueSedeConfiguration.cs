using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proy_back_QBD.Models;

public class EmpaqueSedeConfiguration : IEntityTypeConfiguration<EmpaqueSede>
{
    public void Configure(EntityTypeBuilder<EmpaqueSede> builder)
    {
        builder.ToTable("empaque_sedes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IdSite)
            .HasColumnName("id_sede");

        builder.Property(x => x.IdEmpaque)
            .HasColumnName("id_empaque");

        builder.Property(x => x.Location)
            .HasColumnName("ubicacion");

        builder.Property(x => x.Limite)
            .HasColumnName("limite");

        builder.HasOne(x => x.Sede)
            .WithMany(x => x.EmpaqueSedes)
            .HasForeignKey(x => x.IdSite);

        builder.HasOne(x => x.Empaque)
            .WithMany(x => x.EmpaqueSedes)
            .HasForeignKey(x => x.IdEmpaque);
    }
}
