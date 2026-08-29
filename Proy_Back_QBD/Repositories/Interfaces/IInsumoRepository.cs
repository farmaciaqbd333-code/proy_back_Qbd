public interface ISupplyRepository
{
    Task<SiteSupply> CreateLocationBySiteAsync(SiteSupply insumoSede);
    Task<SiteSupply?> GetSedeSupplyAsync(int idInsumo,int idSede);    
}