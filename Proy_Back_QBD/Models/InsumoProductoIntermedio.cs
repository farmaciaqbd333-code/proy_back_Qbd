using System.ComponentModel.DataAnnotations.Schema;
using proy_back_Qbd.Models.ProductoIntermedio;
using Proy_back_QBD.Models;

public class InsumoProductoIntermedio
{
    public int Id { get; set; }
    public int IdInsumo { get; set; }
    public decimal Porcentaje { get; set; }
    public required string Variable { get; set; }
    public decimal CantidadUnidad { get; set; }
    public decimal FactorCorrecion { get; set; }
    public decimal Dilucion { get; set; }
    public required string UnidadMedida { get; set; }
    public decimal CantidadLote { get; set; }
    public decimal Practica { get; set; }
    public bool Csp { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public Usuario? Creador { get; set; }
    public Usuario? Modificador { get; set; }

    public int IdProductoIntermedio { get; set; }
    public int IdCreador { get; set; }
    public int? IdModificador { get; set; }

    public Insumo? Insumo { get; set; }
    public ProductoIntermedio? ProductoIntermedio { get; set; }

    public List<CompraInsumoProductoIntermedio> CompraInsumoProductoIntermedio { get; set; } = new();
}