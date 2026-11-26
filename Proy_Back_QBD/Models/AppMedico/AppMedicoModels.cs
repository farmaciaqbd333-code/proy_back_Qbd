using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Models.AppMedico
{
    /// <summary>
    /// Acceso del médico a la app móvil (login con CMP + contraseña).
    ///
    /// Va en una tabla aparte y no en <c>medicos</c> porque esa tabla tiene una fila por sede
    /// (el mismo CMP puede estar registrado en Central, Puno, Juliaca...). El acceso es uno solo
    /// por CMP: el médico entra una vez y elige la sede al hacer el pedido.
    /// El CMP se guarda normalizado (sin espacios ni ceros a la izquierda), ver <c>CmpUtil</c>.
    /// </summary>
    [Table("medico_acceso")]
    public class MedicoAcceso
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("cmp")]
        public string Cmp { get; set; } = "";

        /// <summary>Hash BCrypt de la contraseña. Nunca texto plano.</summary>
        [Column("contrasena_hash")]
        public string ContrasenaHash { get; set; } = "";

        [Column("activo")]
        public bool Activo { get; set; } = true;

        /// <summary>true cuando la contraseña la puso el personal: la app pide cambiarla al entrar.</summary>
        [Column("debe_cambiar")]
        public bool DebeCambiar { get; set; } = true;

        /// <summary>Se incrementa al restablecer la contraseña o desactivar el acceso: invalida los tokens anteriores.</summary>
        [Column("version_token")]
        public int VersionToken { get; set; } = 1;

        [Column("intentos_fallidos")]
        public int IntentosFallidos { get; set; }

        [Column("bloqueado_hasta")]
        public DateTime? BloqueadoHasta { get; set; }

        [Column("ultimo_ingreso")]
        public DateTime? UltimoIngreso { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; }

        /// <summary>Usuario del sistema que asignó o cambió el acceso (null si lo cambió el propio médico).</summary>
        [Column("modificador_id")]
        public int? ModificadorId { get; set; }
    }

    /// <summary>
    /// Productos favoritos del médico (relación médico ↔ producto de la lista de precios).
    /// Solo marca favoritos: NO restringe el catálogo. Por defecto el médico ve todo el catálogo.
    /// </summary>
    [Table("medico_producto_favorito")]
    public class MedicoProductoFavorito
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("cmp")]
        public string Cmp { get; set; } = "";

        [Column("lista_precio_id")]
        public int ListaPrecioId { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>
    /// Producto de la Lista de Precios asignado a un médico por el personal QBD (pantalla Lista de Precios).
    /// A diferencia de <see cref="MedicoProductoFavorito"/> (lo marca el médico), esta la decide QBD.
    /// <see cref="MedicoId"/> es la fila de <c>medicos</c> elegida; <see cref="Cmp"/> (normalizado) permite
    /// reconocer al mismo médico en las otras sedes. Tabla: SQL/medico_producto_asignado.sql.
    /// </summary>
    [Table("medico_producto_asignado")]
    public class MedicoProductoAsignado
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("lista_precio_id")]
        public int ListaPrecioId { get; set; }

        [Column("medico_id")]
        public int MedicoId { get; set; }

        [Column("cmp")]
        public string? Cmp { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        /// <summary>Usuario del sistema que hizo la asignación (sale del token).</summary>
        [Column("creador_id")]
        public int? CreadorId { get; set; }
    }

    /// <summary>
    /// Pedido hecho por un médico desde la app. Es una SOLICITUD: el personal la revisa en
    /// "Pedidos de médicos" y la atiende. No es un <see cref="Pedido"/> de Gestión de Pedidos
    /// (ese exige paciente registrado, recibo, cobros y fórmulas).
    /// </summary>
    [Table("pedidos_medico")]
    public class PedidoMedico
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Fila de <c>medicos</c> del médico en la sede elegida (o su primera fila si no está en esa sede).</summary>
        [Column("medico_id")]
        public int MedicoId { get; set; }

        [Column("cmp")]
        public string Cmp { get; set; } = "";

        [Column("sede_id")]
        public int? SedeId { get; set; }

        [Column("paciente_nombre")]
        public string? PacienteNombre { get; set; }

        [Column("paciente_dni")]
        public string? PacienteDni { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = EstadosPedidoMedico.Pendiente;

        /// <summary>Mensaje del personal para el médico (motivo de anulación, aviso de entrega...).</summary>
        [Column("respuesta")]
        public string? Respuesta { get; set; }

        /// <summary>Suma de subtotales con el precio de la Lista A (MED) al momento del pedido.</summary>
        [Column("total")]
        public decimal Total { get; set; }

        /// <summary>Pedido de Gestión de Pedidos en que se convirtió (opcional, lo enlaza el personal).</summary>
        [Column("pedido_id")]
        public int? PedidoId { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; }

        [Column("modificador_id")]
        public int? ModificadorId { get; set; }

        public Medico? Medico { get; set; }
        public Sede? Sede { get; set; }
        public List<PedidoMedicoDetalle> Detalles { get; set; } = new();
    }

    [Table("pedidos_medico_detalle")]
    public class PedidoMedicoDetalle
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("pedido_medico_id")]
        public int PedidoMedicoId { get; set; }

        [Column("lista_precio_id")]
        public int? ListaPrecioId { get; set; }

        /// <summary>Código de la Lista A al momento del pedido.</summary>
        [Column("codigo")]
        public string? Codigo { get; set; }

        /// <summary>Nombre del producto copiado al momento del pedido (si luego cambia la lista, el pedido no cambia).</summary>
        [Column("producto")]
        public string Producto { get; set; } = "";

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_unitario")]
        public decimal PrecioUnitario { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("observacion")]
        public string? Observacion { get; set; }

        public PedidoMedico? PedidoMedico { get; set; }
    }

    /// <summary>Estados del pedido de médico, en orden.</summary>
    public static class EstadosPedidoMedico
    {
        public const string Pendiente = "PENDIENTE";
        public const string Recibido = "RECIBIDO";
        public const string EnProceso = "EN PROCESO";
        public const string Listo = "LISTO";
        public const string Entregado = "ENTREGADO";
        public const string Anulado = "ANULADO";

        public static readonly string[] Todos = { Pendiente, Recibido, EnProceso, Listo, Entregado, Anulado };

        public static bool EsValido(string? estado) =>
            estado != null && Todos.Contains(estado.Trim().ToUpperInvariant());
    }
}
