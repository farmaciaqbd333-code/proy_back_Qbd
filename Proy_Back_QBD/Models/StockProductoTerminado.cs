using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class StockProductoTerminado
{
    public int Id { get; set; }
    public int IdCompraProducto { get; set; }
    public decimal StockDisponible { get; set; }
    public string UnidadMedida { get; set; } = null!;
    public int IdSede { get; set; }

    public CompraProducto CompraProducto { get; set; } = null!;
    public Sede Sede { get; set; } = null!;
}