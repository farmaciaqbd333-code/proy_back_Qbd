// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class PedidoMap : Profile
    {
        public PedidoMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<PedidoCreateReq, Pedido>()
            .ForMember(a => a.Id, o => o.Ignore())
            .ForMember(a => a.Formulas, o => o.Ignore())
            .ForMember(a => a.ProdTerms, o => o.Ignore())
            ;
            CreateMap<PedidoUpdateReq, Pedido>()
            .ForMember(a => a.Id, opt => opt.Ignore())
            .ForMember(a => a.CreadorId, opt => opt.Ignore())
            .ForMember(a => a.Formulas, o => o.Ignore())
            .ForMember(a => a.Estado, o => o.Ignore())
            .ForMember(a => a.ProdTerms, o => o.Ignore())
            ;
            CreateMap<Pedido, PedidoFindIdResponse>()
            .ForMember(a => a.Formulas, o => o.MapFrom(mf => mf.Formulas))
            .ForMember(a => a.ProdTerms, o => o.MapFrom(mf => mf.ProdTerms))
            ;

        }
    }

}