// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class LaboratorioMap : Profile
    {
        public LaboratorioMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<LabCreReq, Laboratorio>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.FormulaId))
            ;
            CreateMap<InsumsCreReq, FormulaCC>()
            ;

        }
    }

}