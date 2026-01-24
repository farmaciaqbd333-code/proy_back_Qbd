// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class PacienteMappingProfile : Profile
    {
        public PacienteMappingProfile()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<PacienteCreateReq, Paciente>();
            CreateMap<PacienteUpdateReq, Paciente>()
            .ForMember(a => a.Id, opt => opt.Ignore())
            .ForMember(a => a.CreadorId, opt => opt.Ignore())
            .ForMember(a => a.SedeId, opt => opt.Ignore())
            ;
            // Otros mapeos si es necesario
        }
    }

}