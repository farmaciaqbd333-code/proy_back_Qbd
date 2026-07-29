using proy_back_Qbd.Models;

public class StockEmpaqueProductoIntermedio
{
    public int Id { get; set; }

    public int IdCompraEmpaque { get; set; }

    public int IdEmpaqueProductoIntermedio { get; set; }

    public decimal Cantidad { get; set; }

    public string UnidadMedida { get; set; } = string.Empty;

    public virtual StockEmpaque StockEmpaque { get; set; } = null!;

    public virtual EmpaqueProductoIntermedio EmpaqueProductoIntermedio { get; set; } = null!;
}