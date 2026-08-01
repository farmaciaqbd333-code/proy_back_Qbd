public class NotaSalidaCreateReq
{
    public DateTimeOffset FechaSalida { get; set; }
    public int IdSedeOrigen { get; set; }
    public int IdSedeDestino { get; set; }
    public string? Observacion { get; set; }
    public int IdCreador { get; set; }
    public List<NotaSalidaFamiliasCreateReq> ListaFamilias { get; set; } = new();
}
public class NotaSalidaFamiliasCreateReq
{
    public int Registro { get; set; }
    public required string Familia { get; set; }
    public decimal Cantidad { get; set; }
    public required string Um { get; set; }
    public int Paquete { get; set; }
    public decimal CantidadPaquete { get; set; }
}
public class FamiliasListaReq
{
    public required string Familia { get; set; }
}