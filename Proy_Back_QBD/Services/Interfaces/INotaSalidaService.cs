public interface INotaSalidaService
{
    Task<int> CrearAsync(NotaSalidaCreateReq request);
}