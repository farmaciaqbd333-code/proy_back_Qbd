using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models.ElaboracionBase
{
    [Table("elaboracion_base")]
    public class ElaboracionBase
    {
        [Column("registro")] public string Registro { get; set; }
        [Column("lote")] public string Lote { get; set; }
        [Column("id_insumo")] public int IdInsumo { get; set; }
        [Column("formula_rapida")] public string FormulaRapida { get; set; }
        [Column("lote_estandar")] public string LoteEstandar { get; set; }
        [Column("tipo")] public string Tipo { get; set; }
        [Column("cantidad")] public decimal Cantidad { get; set; }
        [Column("um")] public string Um { get; set; }
        [Column("fecha_emision")] public DateTime FechaEmision { get; set; }
        [Column("fecha_vencimiento")] public DateTime FechaVencimiento { get; set; }
        [Column("elaborado")] public string Elaborado { get; set; }
        [Column("autorizado")] public string Autorizado { get; set; }
        [Column("procedimiento")] public string Procedimiento { get; set; }
        [Column("cod_me")] public string CodMe { get; set; }
        [Column("cod_termo")] public string CodTermo { get; set; }
        [Column("cod_etiqueta1")] public string CodEtiqueta1 { get; set; }
        [Column("cod_etiqueta2")] public string CodEtiqueta2 { get; set; }
        [Column("cod_adicional")] public string CodAdicional { get; set; }
        [Column("aspecto")] public string Aspecto { get; set; }
        [Column("color")] public string Color { get; set; }
        [Column("olor")] public string Olor { get; set; }
        [Column("ph")] public string Ph { get; set; }
        [Column("usuario")] public string Usuario { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
    }
}