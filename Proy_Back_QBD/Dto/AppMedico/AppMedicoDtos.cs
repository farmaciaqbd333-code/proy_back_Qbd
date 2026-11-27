namespace Proy_back_QBD.Dto.AppMedico
{
    // ===================== Sesión del médico =====================

    public class MedicoLoginReq
    {
        public string? Cmp { get; set; }
        public string? Contrasena { get; set; }
    }

    public class MedicoCambiarContrasenaReq
    {
        public string? Actual { get; set; }
        public string? Nueva { get; set; }
    }

    public class MedicoSedeRes
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        /// <summary>true si el médico está registrado en el directorio de esa sede.</summary>
        public bool Registrado { get; set; }
    }

    public class MedicoPerfilRes
    {
        public string Cmp { get; set; } = "";
        public int MedicoId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Especialidad { get; set; }
        /// <summary>Especialidades de la Lista de Precios que corresponden a la del médico (filtro sugerido del catálogo).</summary>
        public List<string> EspecialidadesSugeridas { get; set; } = new();
        /// <summary>Sede por defecto para los pedidos (la primera en la que está registrado).</summary>
        public int? SedeId { get; set; }
        public bool DebeCambiarContrasena { get; set; }
    }

    public class MedicoLoginRes
    {
        public string Token { get; set; } = "";
        public DateTime Expira { get; set; }
        public MedicoPerfilRes Medico { get; set; } = new();
    }

    // ===================== Catálogo =====================

    /// <summary>Producto del catálogo tal como lo ve el médico: solo el precio de la Lista A (MED).</summary>
    public class CatalogoItemRes
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public string Producto { get; set; } = "";
        public string? Ifas { get; set; }
        public string? FormaFarmaceutica { get; set; }
        public string? Enfermedades { get; set; }
        public string? Indicaciones { get; set; }
        public string? Presentacion { get; set; }
        public string Categoria { get; set; } = "FM";
        public List<string> Especialidades { get; set; } = new();
        /// <summary>Precio Lista A (MED). null = "consultar".</summary>
        public decimal? Precio { get; set; }
        public bool Favorito { get; set; }
        /// <summary>true si QBD le asignó este producto al médico (Lista de Precios → Médicos).</summary>
        public bool Asignado { get; set; }
    }

    public class CatalogoRes
    {
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public List<CatalogoItemRes> Items { get; set; } = new();
    }

    // ===================== Pedidos =====================

    public class PedidoMedicoItemReq
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public string? Observacion { get; set; }
    }

    public class PedidoMedicoCreateReq
    {
        public int? SedeId { get; set; }
        public string? PacienteNombre { get; set; }
        public string? PacienteDni { get; set; }
        public string? Observacion { get; set; }
        public List<PedidoMedicoItemReq> Items { get; set; } = new();
    }

    public class PedidoMedicoDetalleRes
    {
        public int Id { get; set; }
        public int? ProductoId { get; set; }
        public string? Codigo { get; set; }
        public string Producto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public string? Observacion { get; set; }
    }

    public class PedidoMedicoRes
    {
        public int Id { get; set; }
        public string Estado { get; set; } = "";
        public string? Respuesta { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public int? SedeId { get; set; }
        public string? Sede { get; set; }
        public string? PacienteNombre { get; set; }
        public string? PacienteDni { get; set; }
        public string? Observacion { get; set; }
        public decimal Total { get; set; }
        public int CantidadItems { get; set; }
        public int? PedidoId { get; set; }
        /// <summary>Estado del pedido de Gestión de Pedidos enlazado (tal cual; solo para el personal).</summary>
        public string? PedidoEstado { get; set; }
        // Datos del médico (para la vista del personal)
        public int MedicoId { get; set; }
        public string Cmp { get; set; } = "";
        public string? Medico { get; set; }
        public string? Especialidad { get; set; }
        public List<PedidoMedicoDetalleRes> Detalles { get; set; } = new();
    }

    // ===================== Administración (personal QBD) =====================

    public class MedicoAccesoAsignarReq
    {
        /// <summary>Contraseña nueva. Si viene vacía se genera una temporal y se devuelve una sola vez.</summary>
        public string? Contrasena { get; set; }
    }

    public class MedicoAccesoAsignarRes
    {
        public string Cmp { get; set; } = "";
        /// <summary>Solo viene cuando la generó el sistema; no se vuelve a mostrar.</summary>
        public string? ContrasenaTemporal { get; set; }
        public string Message { get; set; } = "";
    }

    public class MedicoAccesoEstadoRes
    {
        public string Cmp { get; set; } = "";
        public bool Activo { get; set; }
        public bool DebeCambiar { get; set; }
        public DateTime? UltimoIngreso { get; set; }
    }

    /// <summary>Paciente con que se registra el pedido del médico en Gestión de Pedidos: el propio médico.</summary>
    public class PacienteDelMedicoRes
    {
        public int PacienteId { get; set; }
        public string? Dni { get; set; }
        public string? Nombre { get; set; }
        /// <summary>true si se acaba de registrar al médico como paciente de la sede.</summary>
        public bool Creado { get; set; }
        public int MedicoId { get; set; }
        public string Cmp { get; set; } = "";
    }

    public class PedidoMedicoEstadoReq
    {
        public string? Estado { get; set; }
        public string? Respuesta { get; set; }
        public int? PedidoId { get; set; }
    }
}
