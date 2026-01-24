// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class AsistenciaMappingProfile : Profile
    {
        public AsistenciaMappingProfile()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<AsistenciaCreateReq, Asistencia>();
            CreateMap<AsistenciaByIdReq, Asistencia>()
            .ForMember(a => a.Id, opt => opt.Ignore())
            .ForMember(a => a.CreadorId, opt => opt.Ignore())
            ;
        }
    }

}