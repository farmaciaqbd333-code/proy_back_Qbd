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
        public DateTimeOffset FechaSalida { get; set; }
        public required string Destino { get; set; }
        public string? Observacion { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public DateTimeOffset FechaModificacion { get; set; }
        public int IdCreador { get; set; }
        public int IdModificador { get; set; }

        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
    }
}