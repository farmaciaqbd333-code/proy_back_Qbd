namespace proy_back_Qbd.Models
{
    public class NotaSalidaDetalleRes
    {
        public int IdNotaSalidaArticulo { get; set; }
        public int IdCompraArticulo { get; set; }
        public string Familia { get; set; } = "MP";
        public string Codigo { get; set; } = "";
        public string DescripcionQBD { get; set; } = "";
        public string Registro { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal? CantidadRecibida { get; set; }
        public string Um { get; set; } = "G";
        public decimal Tara { get; set; }
        public decimal PesoNeto { get; set; }
        public decimal PesoBruto { get; set; }
        public string Lote { get; set; } = "";
        public string FRecib { get; set; } = "";
        public string FFabric { get; set; } = "";
        public string FVcto { get; set; } = "";
        public string? Observacion { get; set; }
        public List<NotaSalidaDetallePaqueteRes> Paquetes { get; set; } = new();
    }

    public class NotaSalidaDetallePaqueteRes
    {
        public int IdPaquete { get; set; }
        public int CantidadPaquete { get; set; }
        public decimal? Peso { get; set; }
        public decimal? Tara { get; set; }
        public string? Um { get; set; }
        public decimal? PesoNeto { get; set; }
        public decimal? PesoBruto { get; set; }
        public int? CantidadPaqueteRecibida { get; set; }
        public decimal? PesoRecibida { get; set; }
        public decimal? TaraRecibida { get; set; }
        public decimal? PesoNetoRecibida { get; set; }
        public decimal? PesoBrutoRecibida { get; set; }
        public int? IdVerificador { get; set; }
    }
}
