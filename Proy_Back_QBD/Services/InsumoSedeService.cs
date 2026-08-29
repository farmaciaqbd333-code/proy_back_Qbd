public class InsumoSedeService : IInsumoSedeService
{
    private readonly IInsumoSedeRepository _repository;

    public InsumoSedeService(IInsumoSedeRepository repository)
    {
        _repository = repository;
    }

    public async Task<InsumoSede> CrearAsync(CrearInsumoSedeDto dto)
    {
        var insumoSede = new InsumoSede
        {
            IdSede = dto.IdSede,
            IdInsumo = dto.IdInsumo,
            Ubicacion = dto.Ubicacion
        };
        return await _repository.CrearAsync(insumoSede);
    }
}