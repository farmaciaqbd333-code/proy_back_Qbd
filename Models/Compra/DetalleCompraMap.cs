using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace proy_back_Qbd.Models
{
    public class DetalleCompraMap : Profile
    {
        public DetalleCompraMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<DetalleOrdenCompraCreateReq, DetalleCompra>(MemberList.None)
            .ForMember(dest => dest.IdInsumo, opt => opt.MapFrom(src => src.IdInsumo))
            .ForMember(dest => dest.DescripcionFac, opt => opt.MapFrom(src => src.DescripcionFac))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal))
            .ForMember(dest => dest.IdCreador, opt => opt.MapFrom(src => src.IdCreador))
            ;
            CreateMap<DetalleOrdenCompraUpdateReq, DetalleCompra>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdInsumo, opt => opt.MapFrom(src => src.IdInsumo))
            .ForMember(dest => dest.DescripcionFac, opt => opt.MapFrom(src => src.DescripcionFac))
            .ForMember(dest => dest.CantidadSolicitada, opt => opt.MapFrom(src => src.Cantidad))
            .ForMember(dest => dest.Um, opt => opt.MapFrom(src => src.Um))
            .ForMember(dest => dest.CostoUnitario, opt => opt.MapFrom(src => src.CostoUnitario))
            .ForMember(dest => dest.CostoTotal, opt => opt.MapFrom(src => src.CostoTotal))
            ;

        }
    }
}