namespace proy_back_Qbd.Dto.NotaSalida
{
    public class CreateReq
    {
        public DateTime FechaSalida { get; set; }
        public int IdSedeOrigen { get; set; }
        public int IdSedeDestino { get; set; }
        public string? Observacion { get; set; }
        public int IdCreador { get; set; }
        public List<NotaSalidaFamiliasCreateReq> ListaFamilias { get; set; } = new();
    }
    public class NotaSalidaFamiliasCreateReq
    {
        public int Registro { get; set; }
        public required string Familia { get; set; }
        public decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public int Paquete { get; set; }
        public decimal CantidadPaquete { get; set; }
        public decimal PesoNeto { get; set; }
        public decimal Tara { get; set; }
        public decimal PesoBruto { get; set; }
    }
    public class ObtenerRegistroReq
    {
        public int IdSede { get; set; }
        public required string Familia { get; set; }
    }
    public class ConfirmarReq
    {
        public int IdSedeOrigen { get; set; }
        public int IdSedeDestino { get; set; }
        public List<ConfirmarArticulosReq> Insumos { get; set; } = new();
        public List<ConfirmarArticulosReq> Economatos { get; set; } = new();
        public List<ConfirmarArticulosReq> Empaques { get; set; } = new();
        public List<ConfirmarArticulosReq> Productos { get; set; } = new();
    }
    public class ConfirmarArticulosReq
    {
        public int IdNotaSalidaArticulo { get; set; }
        public decimal CantidadRecibida { get; set; }
        public int IdCompraArticulo { get; set; }
        public string? UnidadMedida { get; set; }
    }

}