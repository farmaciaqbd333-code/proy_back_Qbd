using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace proy_back_Qbd.Models
{
    public class DetalleCompraLabMap : Profile
    {
        public DetalleCompraLabMap()
        {
            CreateMap<ActualizarDetCompraLabReq, CompraInsumos>(MemberList.None)
            .ForMember(f => f.Coa, o => o.MapFrom(m => m.Coa))
            .ForMember(f => f.Lote, o => o.MapFrom(m => m.Lote))
            .ForMember(f => f.CantidadSolicitada, o => o.MapFrom(m => m.CantidadSolicitada))
            .ForMember(f => f.Potencia, o => o.MapFrom(m => m.Potencia))
            .ForMember(f => f.FechaFabricacion, o => o.MapFrom(m => m.FechaFabricacion))
            .ForMember(f => f.FechaVencimiento, o => o.MapFrom(m => m.FechaVencimiento))
            .ForMember(f => f.CondicionAlmacenamiento, o => o.MapFrom(m => m.CondicionAlmacenamiento))
            .ForMember(f => f.Observacion, o => o.MapFrom(m => m.Observacion))
            .ForMember(f => f.Densidad, o => o.MapFrom(m => m.Densidad));
        }
    }
}