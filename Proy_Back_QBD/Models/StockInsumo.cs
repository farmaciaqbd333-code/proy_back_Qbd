using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class StockInsumo
{
    public int Id { get; set; }
    public int IdCompraInsumo { get; set; }
    public decimal StockDisponible { get; set; }
    public string? UnidadMedida { get; set; }
    public int IdSede { get; set; }
    public int? IdNotaSalidaInsumo { get; set; }

    public CompraInsumos? CompraInsumo { get; set; } 
    public NotaSalidaInsumo? NotaSalidaInsumo { get; set; } 
    public List<AjusteInsumo>? AjusteInsumos { get; set; }
    public List<StockInsumoProductoIntermedio>? StockInsumoProductoIntermedio { get; set; } = null!;
    public Sede Sede { get; set; } = null!;
}