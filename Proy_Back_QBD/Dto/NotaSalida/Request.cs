public class NotaSalidaCreateReq
{
    public DateTimeOffset FechaSalida { get; set; }
    public string Destino { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public int IdCreador { get; set; }
}