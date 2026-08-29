
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;

public class InsumoSedeRepository : IInsumoSedeRepository
{
    private readonly ApiContext _context;
    public InsumoSedeRepository(ApiContext _context)
    {
        this._context = _context;
    }
    public async Task<InsumoSede> CrearAsync(InsumoSede insumoSede)
    {
        await _context.InsumoSedes.AddAsync(insumoSede);
        await _context.SaveChangesAsync();

        return insumoSede;
    }
}