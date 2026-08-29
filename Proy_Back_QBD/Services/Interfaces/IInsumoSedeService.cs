public interface IInsumoSedeService
{
    Task<InsumoSede> CrearAsync(CrearInsumoSedeDto dto);
}