using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace proy_back_Qbd.Models
{
    public class DetalleOrdenCompraMap : Profile
    {
        public DetalleOrdenCompraMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<DetalleInsumosCreateReq, DetalleCompraInsumo>(MemberList.None)
            .ForMember(dest => dest.IdInsumo, opt => opt.MapFrom(src => src.IdInsumo))
            .ForMember(dest => dest.DescripcionFac, opt => opt.MapFrom(src => src.DescripcionFac))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal))
            .ForMember(dest => dest.IdCreador, opt => opt.MapFrom(src => src.IdCreador))
            .ForMember(dest => dest.IdFabricante, opt => opt.MapFrom(src => src.IdFabricante))
            .ForMember(dest => dest.IdFamilia, opt => opt.MapFrom(src => src.IdFamilia))
            ;
            CreateMap<DetalleInsumosUpdateReq, DetalleCompraInsumo>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdInsumo, opt => opt.MapFrom(src => src.IdInsumo))
            .ForMember(dest => dest.IdFamilia, opt => opt.MapFrom(src => src.IdFamilia))
            .ForMember(dest => dest.DescripcionFac, opt => opt.MapFrom(src => src.DescripcionFac))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal))
            .ForMember(dest => dest.IdFabricante, opt => opt.MapFrom(src => src.IdFabricante))
            ;
            CreateMap<ConvertirDetalleCompraReq, DetalleCompraInsumo>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DescripcionFac, opt => opt.MapFrom(src => src.DescripcionFactura))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.FechaFabricacion, opt => opt.MapFrom(src => src.FechaFabricacion))
            .ForMember(dest => dest.FechaVencimiento, opt => opt.MapFrom(src => src.FechaVencimiento))
            .ForMember(dest => dest.Coa, opt => opt.MapFrom(src => src.Coa))
            .ForMember(dest => dest.Lote, opt => opt.MapFrom(src => src.Lote))
            .ForMember(dest => dest.RegistroSanitario, opt => opt.MapFrom(src => src.RegistroSanitario))
            .ForMember(dest => dest.Conformidad, opt => opt.MapFrom(src => src.Conformidad))
            .ForMember(dest => dest.IdFabricante, opt => opt.MapFrom(src => src.IdFabricante))
            ;

            // DetalleCreateReq -> DetalleCompra
            CreateMap<DetalleCreateReq, DetalleCompra>(MemberList.None)
            .ForMember(dest => dest.Clasificacion, opt => opt.MapFrom(src => src.Clasificacion))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.CantidadSolicitada))
            .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleUpdateReq -> DetalleCompra
            CreateMap<DetalleUpdateReq, DetalleCompra>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Clasificacion, opt => opt.MapFrom(src => src.Clasificacion))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.CantidadSolicitada))
            .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEmpaquesCreateReq -> DetalleCompraEmpaque
            CreateMap<DetalleEmpaquesCreateReq, DetalleCompraEmpaque>(MemberList.None)
            .ForMember(dest => dest.IdEmpaque, opt => opt.MapFrom(src => src.IdEmpaque))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEmpaquesUpdateReq -> DetalleCompraEmpaque
            CreateMap<DetalleEmpaquesUpdateReq, DetalleCompraEmpaque>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdEmpaque, opt => opt.MapFrom(src => src.IdEmpaque))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleProductosCreateReq -> DetalleCompraProducto
            CreateMap<DetalleProductosCreateReq, DetalleCompraProducto>(MemberList.None)
            .ForMember(dest => dest.IdProducto, opt => opt.MapFrom(src => src.IdProducto))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleProductosUpdateReq -> DetalleCompraProducto
            CreateMap<DetalleProductosUpdateReq, DetalleCompraProducto>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdProducto, opt => opt.MapFrom(src => src.IdProducto))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEconomatosCreateReq -> DetalleCompraEconomato
            CreateMap<DetalleEconomatosCreateReq, DetalleCompraEconomato>(MemberList.None)
            .ForMember(dest => dest.IdEconomato, opt => opt.MapFrom(src => src.IdEconomato))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEconomatosUpdateReq -> DetalleCompraEconomato
            CreateMap<DetalleEconomatosUpdateReq, DetalleCompraEconomato>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdEconomato, opt => opt.MapFrom(src => src.IdEconomato))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

        }
    }
}