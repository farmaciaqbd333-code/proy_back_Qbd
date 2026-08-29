
using Proy_back_QBD.Models;

public class SiteSupply
{
    public int Id { get; set; }
    public int IdSite { get; set; }
    public int IdSupply { get; set; }
    public string? Location { get; set; }

    public virtual Sede Sede { get; set; } = null!;
    public virtual Insumo Insumo { get; set; } = null!;
}