// Entidad
using Proy_back_QBD.Models;

public class FormulaRapidaSede
{
    public int Id { get; set; }
    public int IdFormular { get; set; }
    public int IdSede { get; set; }

    public virtual FormulaR FormulaR { get; set; } = null!;
    public virtual Sede Sede { get; set; } = null!;
}