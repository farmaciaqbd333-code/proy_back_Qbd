using AutoMapper;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Profiles
{
    public class CotizacionMap : Profile
    {
        public CotizacionMap()
        {
            CreateMap<Cotizacion, CotizacionRes>()
                .ForMember(dest => dest.Fecha, opt => opt.MapFrom(src => src.Fecha.ToString("dd/MM/yyyy")))
                .ForMember(dest => dest.TotalOnerosa, opt => opt.MapFrom(src => src.Total))
                .ForMember(dest => dest.TotalGratuita, opt => opt.MapFrom(src => src.OpGratuita))
                .ForMember(dest => dest.Saldo, opt => opt.MapFrom(src => src.Total - src.Adelanto))
                .ForMember(dest => dest.SedeNombre, opt => opt.MapFrom(src => src.Sede != null ? src.Sede.Nombre : null))
                .ForMember(dest => dest.UsuarioCreador, opt => opt.MapFrom(src => src.Creador != null ? src.Creador.Codigo : null))
                .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.Detalles));

            CreateMap<CotizacionDetalle, CotizacionDetalleRes>();

            CreateMap<CotizacionCreateReq, Cotizacion>()
                .ForMember(dest => dest.Detalles, opt => opt.Ignore())
                .ForMember(dest => dest.Adelanto, opt => opt.Ignore())
                .ForMember(dest => dest.AdelantoMedioPago, opt => opt.Ignore())
                .ForMember(dest => dest.AdelantoFecha, opt => opt.Ignore())
                .ForMember(dest => dest.PedidoId, opt => opt.Ignore());

            CreateMap<CotizacionDetalleReq, CotizacionDetalle>();
        }
    }
}
