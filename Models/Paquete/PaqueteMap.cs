using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace proy_back_Qbd.Models
{
    public class PaqueteMap : Profile
    {
        public PaqueteMap()
        {
            CreateMap<CrearPaqueteReq, Paquete>()
            .ForMember(f => f.Id, f => f.Ignore())
            .ForMember(f => f.IdModificador, f => f.Ignore())
            ;
            CreateMap<ModificarPaqueteReq, Paquete>()
            .ForMember(f => f.Id, f => f.Ignore())
            .ForMember(f => f.IdDetalleCompra, f => f.Ignore())
            ;
        }
    }
}