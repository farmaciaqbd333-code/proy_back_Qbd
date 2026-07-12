using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class AjusteEconomato
{
    public int Id { get; set; }

    public decimal Ajuste { get; set; }

    public int IdCompraEconomato { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public DateTimeOffset? FechaModificacion { get; set; }

    public int IdCreador { get; set; }

    public int? IdModificador { get; set; }

    public decimal StockAnterior { get; set; }

    public decimal StockNuevo { get; set; }

    public string? Observacion { get; set; }

    public virtual CompraEconomatos CompraEconomato { get; set; } = null!;

    public virtual Usuario Creador { get; set; } = null!;
}