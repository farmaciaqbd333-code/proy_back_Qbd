using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    public class NotaSalidaProducto
    {
        public int IdNotaSalida { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CantidadRecibida { get; set; }
        public string? Um { get; set; }
        public string? Lote { get; set; }
        public string? Observacion { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public int IdCreador { get; set; }
        public int Id { get; set; }
        public int? IdModificador { get; set; }
        public DateTimeOffset? FechaModificacion { get; set; }
        public int? IdCompraProducto { get; set; }

        public Usuario? Creador { get; set; }
        public CompraProducto? CompraProducto { get; set; }
        public StockProducto? StockProductoTerminado { get; set; }
        public NotaSalida? NotaSalida { get; set; }
        public Usuario? Modificador { get; set; }
        public List<PaqueteNotaSalidaProducto>? PaqueteNotaSalidaProductos { get; set; }
    }
}