// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class FormulaMap : Profile
    {
        public FormulaMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<FormulaCreatePedido, Formula>()
            .ForMember(a => a.Id, o => o.Ignore())
            .ForMember(a => a.ModificadorId, o => o.Ignore())
            .ForMember(a => a.PedidoId, o => o.Ignore())
            ;
            CreateMap<FormulaCreateReq, Formula>()
            ;
            CreateMap<FormulaUpdateReq, Formula>()
            .ForMember(a => a.Id, opt => opt.Ignore())
            .ForMember(a => a.CreadorId, opt => opt.Ignore())
            ;
            CreateMap<FormulaUpdateReq, Formula>()
            .ForMember(a => a.Id, opt => opt.Ignore())
            .ForMember(a => a.CreadorId, opt => opt.Ignore())
            ;
            CreateMap<Formula, FormulasByPedido>()
            ;
        }
    }

}