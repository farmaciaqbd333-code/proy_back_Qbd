public interface IInsumoSedeRepository
{
    Task<InsumoSede> CrearAsync(InsumoSede insumoSede);
    
}