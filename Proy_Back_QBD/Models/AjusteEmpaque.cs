using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace proy_back_Qbd.Models
{
    public class AjusteEmpaque
    {
        public int Id { get; set; }

        public decimal Ajuste { get; set; }

        public decimal StockAnterior { get; set; }

        public decimal StockNuevo { get; set; }

        public int IdStockEmpaque { get; set; }

        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.Now;

        public int IdCreador { get; set; }

        public string? Observacion { get; set; }

        public StockEmpaque? StockEmpaque { get; set; }

        public Usuario? Creador { get; set; }
    }
}