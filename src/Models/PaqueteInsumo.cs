using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("paquete_insumo")]
    public class PaqueteInsumo
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("id_paquete")] public int IdPaquete { get; set; }
        [Column("id_compra_insumo")] public int IdCompraInsumo { get; set; }
        public Paquete? Paquete { get; set; }
        public CompraInsumos? CompraInsumo { get; set; }
    }
}