using AutoMapper;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Profiles
{
    public class ProductoMap : Profile
    {
        public ProductoMap()
        {
            CreateMap<ProductoReq, Producto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
                .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore())
                .ForMember(dest => dest.Creador, opt => opt.Ignore())
                .ForMember(dest => dest.Modificador, opt => opt.Ignore())
                .ForMember(dest => dest.ProdTerm, opt => opt.Ignore());
        }
    }
}
