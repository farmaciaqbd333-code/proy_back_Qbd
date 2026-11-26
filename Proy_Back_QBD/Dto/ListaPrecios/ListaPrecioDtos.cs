namespace Proy_back_QBD.Dto.ListaPrecios
{
    /// <summary>Datos de un producto de la lista de precios.</summary>
    public class ListaPrecioReq
    {
        public string? CodigoS { get; set; }
        public string? CodigoA { get; set; }
        public string? CodigoB { get; set; }
        public string? CodigoC { get; set; }
        public string? Producto { get; set; }
        public string? Ifas { get; set; }
        public decimal? PrecioS { get; set; }
        public decimal? PrecioA { get; set; }
        public decimal? PrecioB { get; set; }
        public decimal? PrecioC { get; set; }
        public string? Especialidad1 { get; set; }
        public string? Especialidad2 { get; set; }
        public string? Especialidad3 { get; set; }
        public string? Indicaciones { get; set; }
        public string? Presentacion { get; set; }
        public string? FormaFarmaceutica { get; set; }
        public string? Enfermedades { get; set; }
        public string? Categoria { get; set; }
        public string? ModoUso { get; set; }
        public string? Precauciones { get; set; }
        public string? Observaciones { get; set; }
    }

    /// <summary>Fila del Excel de importación.</summary>
    public class ListaPrecioImportFila : ListaPrecioReq
    {
        public int? Fila { get; set; }
        public int? Id { get; set; }
    }

    /// <summary>Importación masiva desde Excel.</summary>
    public class ListaPrecioImportReq
    {
        public List<ListaPrecioImportFila> Filas { get; set; } = new();
    }

    /// <summary>Cantidad de productos por especialidad.</summary>
    public record EspecialidadConteoRes(string Especialidad, int Total);

    /// <summary>Resultado de la importación desde Excel.</summary>
    public record ListaPrecioImportRes(int Creados, int Actualizados, int SinCambios, List<string> Errores, string Message);
}

namespace Proy_back_QBD.Dto.ListaPrecios
{
    /// <summary>Médico del directorio (todas las sedes) para el selector de Lista de Precios.</summary>
    public class MedicoAsignableRes
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? Cmp { get; set; }
        public int SedeId { get; set; }
        public string? Sede { get; set; }
        public string? Especialidad { get; set; }
    }

    /// <summary>Médico asignado a un producto de la Lista de Precios.</summary>
    public class ProductoMedicoRes
    {
        public int ProductoId { get; set; }
        public int MedicoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Cmp { get; set; }
        public string? Sede { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>Médicos a asignar a un producto (se agregan a los que ya tenga).</summary>
    public class ProductoMedicosAsignarReq
    {
        public List<int> MedicoIds { get; set; } = new();
    }
}
