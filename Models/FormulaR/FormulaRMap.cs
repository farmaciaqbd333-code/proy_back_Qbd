// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class FormulaRMap : Profile
    {
        public FormulaRMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<FormularRReq, FormulaR>()
            .ForMember(a => a.Id, o => o.Ignore())
            .ForMember(a => a.ModificadorId, o => o.Ignore())
            ;
            CreateMap<FormularRUpdTReq, FormulaR>()
            .ForMember(a => a.Id, o => o.Ignore())
            .ForMember(a => a.CreadorId, o => o.Ignore())
            ;

        }
    }

}