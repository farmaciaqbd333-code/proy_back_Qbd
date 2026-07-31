public interface INotaSalidaService
{
    Task<int> CrearAsync(NotaSalidaCreateReq request);
    Task<List<RegistrosListaRes>> ObtenerRegistros(int idArticulo, string Familia, int idSede);
    Task<List<NotaSalidaListaRes>> ObtenerListaAsync(int idSede);
    Task ActualizarAsync(int id, NotaSalidaCreateReq request);
    Task EliminarAsync(int id);
}