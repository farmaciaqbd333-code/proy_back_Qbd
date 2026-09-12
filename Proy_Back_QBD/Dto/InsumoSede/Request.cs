public class AssignLocationReq
{
    public int IdSede { get; set; }
    public int IdInsumo { get; set; }
    public string? Ubicacion { get; set; }
    public string? Familia { get; set; }
}

public class AssignLimiteReq
{
    public int IdSede { get; set; }
    public int IdInsumo { get; set; }
    public decimal? Limite { get; set; }
    public string? Familia { get; set; }
}
