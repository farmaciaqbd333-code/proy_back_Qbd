namespace proy_back_Qbd.Dto.Recepcion
{
    public class RecepcionNotaSalidaReq
    {
        public string? Estado { get; set; }
        public DateTime FechaRecepcion { get; set; }
        public int IdUsuarioRecepcion { get; set; }
        public List<RecepcionFamiliaReq> Familias { get; set; } = new();
    }

    public class RecepcionFamiliaReq
    {
        public required string Familia { get; set; }
        public int IdNotaSalidaFamilia { get; set; }
        public decimal CantidadRecibida { get; set; }
        public List<RecepcionPaqueteReq> Paquetes { get; set; } = new();
    }

    public class RecepcionPaqueteReq
    {
        public int IdPaquete { get; set; }
        public int CantidadPaqueteRecibida { get; set; }
        public decimal PesoRecibida { get; set; }
        public decimal TaraRecibida { get; set; }
        public decimal PesoNetoRecibida { get; set; }
        public decimal PesoBrutoRecibida { get; set; }
        public int IdVerificador { get; set; }
    }
}
