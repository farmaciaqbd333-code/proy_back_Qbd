using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class StockEconomato
{
    public int Id { get; set; }
    public int IdCompraEconomato { get; set; }
    public decimal StockDisponible { get; set; }
    public string UnidadMedida { get; set; } = null!;
    public int IdSede { get; set; }

    public CompraEconomatos CompraEconomato { get; set; } = null!;
    public Sede Sede { get; set; } = null!;
}