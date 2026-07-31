public interface INotaSalidaService
{
    Task<int> CrearAsync(NotaSalidaCreateReq request);
    Task<List<NotaSalidaListaRes>> ObtenerListaAsync(int idSede);
}