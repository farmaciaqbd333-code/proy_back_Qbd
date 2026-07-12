// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class InsumoRMap : Profile
    {
        public InsumoRMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<InsumoRCreateReq, InsumoR>()
            .ForMember(a => a.ModificadorId, o => o.Ignore())
            ;
            CreateMap<InsumoRUpdateReq, InsumoR>()
            .ForMember(a => a.CreadorId, o => o.Ignore())
            ;

        }
    }

}