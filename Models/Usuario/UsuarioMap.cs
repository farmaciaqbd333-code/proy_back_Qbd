// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class UsuarioMappingProfile : Profile
    {
        public UsuarioMappingProfile()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<UsuarioUpdateReq, Usuario>()
            .ForMember(a => a.Id, options => options.Ignore())
            .ForMember(a => a.FechaCreacion, options => options.Ignore())
            .ForMember(a => a.CreadorId, options => options.Ignore())
            .ForMember(a => a.Tipo, options => options.Ignore())
            .ForMember(a => a.Persona, options => options.Ignore())
            .ForMember(a => a.PersonaId, options => options.Ignore())
            ;
             CreateMap<UsuarioCreateReq, Usuario>()
            .ForMember(a => a.Id, options => options.Ignore())
            .ForMember(a => a.FechaCreacion, options => options.Ignore())
            ;
            // Otros mapeos si es necesario
        }
    }

}