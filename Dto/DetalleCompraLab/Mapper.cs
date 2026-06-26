using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Riok.Mapperly.Abstractions;

namespace proy_back_Qbd.Models.DetalleCompraLab
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public partial class DetalleCompraLabMapper
    {
        public partial void ActualizarInsumo(ActualizarInsumoReq source, CompraInsumos target);
        public partial void ActualizarEmpaque(ActualizarEmpaqueReq source, CompraEmpaques target);

    }
}