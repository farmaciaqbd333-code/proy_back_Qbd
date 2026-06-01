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

            CreateMap<DetalleInsumosUpdateReq, CompraInsumos>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdInsumo, opt => opt.MapFrom(src => src.IdInsumo))
            .ForMember(dest => dest.DescripcionFactura, opt => opt.MapFrom(src => src.DescripcionFactura))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.CantidadSolicitada))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal))
            .ForMember(dest => dest.IdFabricante, opt => opt.MapFrom(src => src.IdFabricante))
            ;

            // DetalleCreateReq -> DetalleCompra
            CreateMap<DetalleOtrosReq, CompraOtros>(MemberList.None)
            .ForMember(dest => dest.Clasificacion, opt => opt.MapFrom(src => src.Clasificacion))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.CantidadSolicitada))
            .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleUpdateReq -> DetalleCompra
            CreateMap<DetalleUpdateReq, CompraOtros>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Clasificacion, opt => opt.MapFrom(src => src.Clasificacion))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.CantidadSolicitada))
            .ForMember(dest => dest.UnidadMedida, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEmpaquesCreateReq -> DetalleCompraEmpaque
            CreateMap<DetalleEmpaquesCreateReq, CompraEmpaques>(MemberList.None)
            .ForMember(dest => dest.IdEmpaque, opt => opt.MapFrom(src => src.IdEmpaque))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEmpaquesUpdateReq -> DetalleCompraEmpaque
            CreateMap<DetalleEmpaquesUpdateReq, CompraEmpaques>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdEmpaque, opt => opt.MapFrom(src => src.IdEmpaque))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleProductosCreateReq -> DetalleCompraProducto
            CreateMap<DetalleProductosCreateReq, CompraProductos>(MemberList.None)
            .ForMember(dest => dest.IdProducto, opt => opt.MapFrom(src => src.IdProducto))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleProductosUpdateReq -> DetalleCompraProducto
            CreateMap<DetalleProductosUpdateReq, CompraProductos>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdProducto, opt => opt.MapFrom(src => src.IdProducto))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEconomatosCreateReq -> DetalleCompraEconomato
            CreateMap<DetalleEconomatosCreateReq, CompraEconomatos>(MemberList.None)
            .ForMember(dest => dest.IdEconomato, opt => opt.MapFrom(src => src.IdEconomato))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

            // DetalleEconomatosUpdateReq -> DetalleCompraEconomato
            CreateMap<DetalleEconomatosUpdateReq, CompraEconomatos>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdEconomato, opt => opt.MapFrom(src => src.IdEconomato))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal));

        }
    }
}