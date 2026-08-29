public interface IInsumoSedeService
{
    Task<SiteSupply> CrearAsync(AssignLocationReq dto);
}