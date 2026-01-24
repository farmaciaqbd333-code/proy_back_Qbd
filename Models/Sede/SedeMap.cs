// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class SedeMappingProfile : Profile
    {
        public SedeMappingProfile()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<SedeCreateReq, Sede>();
            CreateMap<SedeUpdateReq, Sede>()
            .ForMember(a => a.Id, options => options.Ignore())
            .ForMember(a => a.FechaCreacion, options => options.Ignore())
            .ForMember(a => a.CreadorId, options => options.Ignore())
            ;
            // Otros mapeos si es necesario
        }
    }

}