// Configuration
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FormulaRapidaSedeConfiguration : IEntityTypeConfiguration<FormulaRapidaSede>
{
    public void Configure(EntityTypeBuilder<FormulaRapidaSede> builder)
    {
        builder.ToTable("formula_rapida_sede");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd(); ;
        builder.Property(x => x.IdFormular).HasColumnName("id_formula_rapida");
        builder.Property(x => x.IdSede).HasColumnName("id_sede");

        builder.HasOne(x => x.FormulaR)
            .WithMany(wm => wm.FormulaRSedes)
            .HasForeignKey(x => x.IdFormular);

        builder.HasOne(x => x.Sede)
            .WithMany(wm => wm.FormulaRSedes)
            .HasForeignKey(x => x.IdSede);
    }
}