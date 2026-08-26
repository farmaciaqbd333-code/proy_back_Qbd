using Proy_back_QBD.Data;

public class UnitWork : IUnitOfWork
{
    private readonly ApiContext _context;
    public UnitWork(ApiContext apiContext)
    {
        this._context = apiContext;
    }
    public async Task GuardarCambios()
    {
        await _context.SaveChangesAsync();
    }

}