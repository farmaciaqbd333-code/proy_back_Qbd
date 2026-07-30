using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class StockEmpaque
{
    public int Id { get; set; }
    public int IdCompraEmpaque { get; set; }
    public decimal StockDisponible { get; set; }
    public string UnidadMedida { get; set; } = null!;
    public int IdSede { get; set; }
    public int? IdNotaSalidaEmpaque { get; set; }
    public CompraEmpaque CompraEmpaque { get; set; } = null!;
    public NotaSalidaEmpaque NotaSalidaEmpaque { get; set; } = null!;
    public List<StockEmpaqueProductoIntermedio> StockEmpaqueProductoIntermedio { get; set; } = null!;
    public Sede Sede { get; set; } = null!;
}