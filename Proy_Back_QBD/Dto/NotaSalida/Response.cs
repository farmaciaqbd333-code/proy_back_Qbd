public class NotaSalidaListaRes
{
    public int IdNotaSalida { get; set; }
    public required string Codigo { get; set; }
    public required DateTimeOffset FechaCreacion { get; set; }
    public required string Destino { get; set; }
    public required string Responsable { get; set; }
    public string? Observacion { get; set; }
}
public class RegistrosListaRes
{
    public int IdRegistro { get; set; }
    public required string Registro { get; set; }
    public required string Codigo { get; set; }
}