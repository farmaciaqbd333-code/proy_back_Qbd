namespace proy_back_Qbd.Models;

public class PaqueteNotaSalidaProducto
{
    public long Id { get; set; }
    public long IdNotaSalidaProducto { get; set; }
    public int CantidadPaquete { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Tara { get; set; }
    public string? Um { get; set; }
    public decimal? PesoNeto { get; set; }
    public decimal? PesoBruto { get; set; }
}
