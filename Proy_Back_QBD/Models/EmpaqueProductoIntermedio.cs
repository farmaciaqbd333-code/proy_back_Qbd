using proy_back_Qbd.Models.ProductoIntermedio;
using Proy_back_QBD.Models;

public class EmpaqueProductoIntermedio
{
    public int Id { get; set; }

    public int IdEmpaque { get; set; }

    public int IdProductoIntermedio { get; set; }
    public virtual Empaque? Empaque { get; set; }

    public virtual ProductoIntermedio? ProductoIntermedio { get; set; }
    public List<CompraEmpaqueProductoIntermedio> CompraEmpaqueProductoIntermedios { get; set; } = new();
}