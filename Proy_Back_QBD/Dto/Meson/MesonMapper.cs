using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models;
using Riok.Mapperly.Abstractions;

namespace proy_back_Qbd.Dto.Meson
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public partial class MesonMapper
    {
        public partial void ActualizarInsumos(MesonDetInsumoConvReq source, CompraInsumos target);
        public partial void ActualizarOtros(MesonDetOtrosConvReq source, CompraOtros target);
        public partial void ActualizarEmpaques(MesonDetEmpaqueConvReq source, CompraEmpaques target);
        public partial void ActualizarEconomatos(MesonDetEconomatoConvReq source, CompraEconomatos target);
        public partial void ActualizarProductos(MesonDetProductoConvReq source, CompraProductos target);
    }

}