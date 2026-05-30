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
        public partial void ActualizarInsumos(MesonDetInsumoConvReq source, DetalleCompraInsumo target);
        public partial void ActualizarOtros(MesonDetOtrosConvReq source, DetalleCompraOtros target);
        public partial void ActualizarEmpaques(MesonDetEmpaqueConvReq source, DetalleCompraEmpaque target);
        public partial void ActualizarEconomatos(MesonDetEconomatoConvReq source, DetalleCompraEconomato target);
        public partial void ActualizarProductos(MesonDetProductoConvReq source, DetalleCompraProducto target);
    }

}