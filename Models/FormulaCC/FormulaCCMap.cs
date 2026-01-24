// ApoderadoMappingProfile.cs
using AutoMapper;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request; // Asegúrate de incluir el espacio de nombres correcto

namespace Proy_back_QBD.Profiles
{
    public class FormulaCCMap : Profile
    {
        public FormulaCCMap()
        {
            // Mapeo entre ApoderadoCreate y Apoderado
            CreateMap<FormulaCCUpdReq, FormulaCC>()
            ;
            CreateMap<FormulaCreateReq, FormulaCC>()
            ;

        }
    }

}