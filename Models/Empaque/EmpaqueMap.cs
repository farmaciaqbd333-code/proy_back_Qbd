// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Empaque;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Profiles
{
    public class EmpaqueMap : Profile
    {
        public EmpaqueMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<EmpaqueCreateReq, Empaque>()
            .ForMember(a => a.Id, o => o.Ignore())
            .ForMember(a => a.ModificadorId, o => o.Ignore())
            .ForMember(dest => dest.IdFunda, opt => opt.MapFrom(src => src.FundaId))
            .ForMember(dest => dest.IdCaja, opt => opt.MapFrom(src => src.CajaId))
            .ForMember(dest => dest.IdEtiqueta1, opt => opt.MapFrom(src => src.EtiquetaId1))
            .ForMember(dest => dest.IdEtiqueta2, opt => opt.MapFrom(src => src.EtiquetaId2));
            CreateMap<EmpaqueUpdateReq, Empaque>()
            .ForMember(a => a.Id, o => o.Ignore())
            .ForMember(a => a.CreadorId, o => o.Ignore());

        }
    }

}