using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class StockProducto
{
    public int Id { get; set; }
    public int IdCompraProducto { get; set; }
    public decimal StockDisponible { get; set; }
    public string? UnidadMedida { get; set; }
    public int IdSede { get; set; }
    public int? IdNotaSalidaProducto { get; set; }
    public CompraProducto CompraProducto { get; set; } = null!;
    public NotaSalidaProducto NotaSalidaProducto { get; set; } = null!;
    public List<AjusteProducto>? AjusteProductos { get; set; }
    public Sede Sede { get; set; } = null!;
}