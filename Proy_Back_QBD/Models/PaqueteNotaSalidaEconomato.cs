namespace proy_back_Qbd.Models;

public class PaqueteNotaSalidaEconomato
{
    public long Id { get; set; }
    public int IdNotaSalidaEconomato { get; set; }
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
    public Proy_back_QBD.Models.Usuario? Verificador { get; set; }
    public int? IdCreador { get; set; }
    public Proy_back_QBD.Models.Usuario? Creador { get; set; }
    
    public NotaSalidaEconomato? NotaSalidaEconomato { get; set; }
}
