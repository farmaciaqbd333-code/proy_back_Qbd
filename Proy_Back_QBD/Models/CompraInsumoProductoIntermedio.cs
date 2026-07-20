using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class CompraInsumoProductoIntermedio
{
    public int Id { get; set; }

    public int IdInsumoProductoIntermedio { get; set; }

    public decimal Cantidad { get; set; }

    public string UnidadMedida { get; set; } = "G";

    public DateTime FechaCreacion { get; set; }

    public int IdCreador { get; set; }

    public int IdCompraInsumo { get; set; }

    public int? IdModificador { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public InsumoProductoIntermedio? InsumoProductoIntermedio { get; set; }

    public CompraInsumos? CompraInsumo { get; set; }

    public Usuario? Creador { get; set; }

    public Usuario? Modificador { get; set; }
}