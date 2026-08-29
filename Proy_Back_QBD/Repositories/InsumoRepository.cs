
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using Proy_back_QBD.Data;

public class SupplyRepository : ISupplyRepository
{
    private readonly ApiContext _context;
    public SupplyRepository(ApiContext _context)
    {
        this._context = _context;
    }
    public async Task<SiteSupply> CreateLocationBySiteAsync(SiteSupply siteSupply)
    {
        await _context.SiteSupplies.AddAsync(siteSupply);
        return siteSupply;
    }
    public async Task<SiteSupply?> GetSedeSupplyAsync(int idInsumo, int idSede)
    {
        SiteSupply? siteSupply = await _context.SiteSupplies
        .Where(w => w.IdSupply == idInsumo && w.IdSite == idSede)
        .FirstOrDefaultAsync();

        if (siteSupply == null)
        {
            return null;
        }
        
        return siteSupply;
    }
}