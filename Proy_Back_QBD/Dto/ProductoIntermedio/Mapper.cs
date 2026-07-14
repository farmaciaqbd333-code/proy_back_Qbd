using proy_back_Qbd.Models.ProductoIntermedio;
using Proy_back_QBD.Request;
using Riok.Mapperly.Abstractions;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class ProductoIntermedioMapper
{
    public partial ProductoIntermedio CrearProductoIntermedio(CrearProductoIntermedioReq source);
    public partial InsumoProductoIntermedio CrearInsumosProductoIntermedio(InsumoProductoIntermedioReq source);
}