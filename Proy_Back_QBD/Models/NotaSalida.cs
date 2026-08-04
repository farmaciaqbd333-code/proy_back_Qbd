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
    public class NotaSalida
    {
        public int Id { get; set; }
        public DateTime FechaSalida { get; set; }
        public int IdSedeOrigen { get; set; }
        public int IdSedeDestino { get; set; }
        public string? Observacion { get; set; }
        public required string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int IdCreador { get; set; }
        public int? IdModificador { get; set; }

        public Usuario? Creador { get; set; }
        public Sede? SedeOrigen { get; set; }
        public Sede? SedeDestino { get; set; }
        public Usuario? Modificador { get; set; }
        public List<NotaSalidaInsumo>? NotaSalidaInsumos { get; set; }
        public List<NotaSalidaEconomato>? NotaSalidaEconomatos { get; set; }
        public List<NotaSalidaEmpaque>? NotaSalidaEmpaques { get; set; }
        public List<NotaSalidaProducto>? NotaSalidaProductos { get; set; }
    }
}