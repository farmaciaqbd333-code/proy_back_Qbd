
using Proy_back_QBD.Models;

public class InsumoSede
{
    public int Id { get; set; }
    public int IdSede { get; set; }
    public int IdInsumo { get; set; }
    public string? Ubicacion { get; set; }

    public virtual Sede sede { get; set; } = null!;
    public virtual Insumo insumo { get; set; } = null!;
}