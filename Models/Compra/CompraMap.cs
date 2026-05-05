
using AutoMapper;

namespace proy_back_Qbd.Models
{
    public class CompraMap : Profile
    {
        public CompraMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<OrdenCompraCreateReq, Compra>(MemberList.None)
            .ForMember(dest => dest.IdProveedor, opt => opt.MapFrom(src => src.IdProveedor))
            .ForMember(dest => dest.Modalidad, opt => opt.MapFrom(src => src.Modalidad))
            .ForMember(dest => dest.Moneda, opt => opt.MapFrom(src => src.Moneda))
            .ForMember(dest => dest.TipoCambio, opt => opt.MapFrom(src => src.TipoCambio))
            .ForMember(dest => dest.Igv, opt => opt.MapFrom(src => src.Igv))
            .ForMember(dest => dest.FechaCotizacion, opt => opt.MapFrom(src => src.FechaCotizacion))
            .ForMember(dest => dest.Observaciones, opt => opt.MapFrom(src => src.Observaciones))
            .ForMember(dest => dest.IdSede, opt => opt.MapFrom(src => src.IdSede))
            .ForMember(dest => dest.IdCreador, opt => opt.MapFrom(src => src.IdCreador))
            .ForMember(dest => dest.Familia, opt => opt.MapFrom(src => src.Familia))
            .ForMember(dest => dest.EstadoCompra, opt => opt.MapFrom(src => "Pendiente"));
            ;
            CreateMap<OrdenCompraUpdateReq, Compra>(MemberList.None)
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IdProveedor, opt => opt.MapFrom(src => src.IdProveedor))
            .ForMember(dest => dest.Modalidad, opt => opt.MapFrom(src => src.Modalidad))
            .ForMember(dest => dest.Moneda, opt => opt.MapFrom(src => src.Moneda))
            .ForMember(dest => dest.TipoCambio, opt => opt.MapFrom(src => src.TipoCambio))
            .ForMember(dest => dest.Igv, opt => opt.MapFrom(src => src.Igv))
            .ForMember(dest => dest.FechaCotizacion, opt => opt.MapFrom(src => src.FechaCotizacion))
            .ForMember(dest => dest.Observaciones, opt => opt.MapFrom(src => src.Observaciones))
            .ForMember(dest => dest.IdSede, opt => opt.MapFrom(src => src.IdSede))
            .ForMember(dest => dest.IdModificador, opt => opt.MapFrom(src => src.IdModificadorCreador))
            .ForMember(dest => dest.Familia, opt => opt.MapFrom(src => src.Familia))
            ;

        }
    }
}