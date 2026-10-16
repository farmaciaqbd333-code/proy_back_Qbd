using System;
using System.Collections.Generic;

namespace proy_back_Qbd.Models.Kardex
{
    public class ResolverLotesFormulaReq
    {
        public int IdSede { get; set; }
        public List<InsumoFormulaItemReq> Insumos { get; set; } = new();
    }

    public class InsumoFormulaItemReq
    {
        public int Index { get; set; }
        public int IdInsumo { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        public decimal CantidadRequerida { get; set; }
        public string? Tipo { get; set; }
    }

    public class LoteAsignadoRes
    {
        public int Index { get; set; }
        public int IdInsumo { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        public string Registro { get; set; } = "---";
        public string Lote { get; set; } = "---";
        public decimal StockDisponible { get; set; }
        public string TipoInsumo { get; set; } = "MP";
        public List<DetalleInsumoRes> LotesDisponibles { get; set; } = new();
    }
}
