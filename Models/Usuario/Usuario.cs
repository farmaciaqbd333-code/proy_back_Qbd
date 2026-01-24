using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{

    [Table("usuarios")]
    public class Usuario
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }  // Puede ser nulo 
        [Column("contrasena")]
        public required string Contrasena { get; set; }  // Puede ser nulo        
        [JsonIgnore]
        public TipoUsuario? Tipo { get; set; }  // Puede ser nulo
        [Column("tipo_id")]
        public int? TipoId { get; set; }  // Puede ser nulo
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; }  // Puede ser nulo        
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_modificacion")]
        public DateTime? FechaModificacion { get; set; }  // Puede ser nulo
        [Column("creador_id")]
        public int? CreadorId { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [Column("modificador_id")]
        public int? ModificadorId { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [Column("horario_entrada")]
        public TimeOnly? HorarioEntrada { get; set; }  // Puede ser nulo
        [Column("horario_salida")]
        public TimeOnly? HorarioSalida { get; set; }  // Puede ser nulo
        [JsonIgnore]
        public Persona? Persona { get; set; }  // Puede ser nulo
        [Column("persona_id")]
        public int? PersonaId { get; set; }  // Puede ser nulo
        [Column("cqfp")]
        public string? CQFP { get; set; }  // Puede ser nulo
        [Column("sedeId")]
        public int? SedeId { get; set; }  // Puede ser nulo
        [JsonIgnore]
        public Sede? Sede { get; set; }  // Puede ser nulo
        [Column("horario_almuerzo")]
        public TimeOnly? HorarioAlmuerzo { get; set; }  // Puede ser nulo
        [Column("horario_regreso")]
        public TimeOnly? HorarioRegreso { get; set; }  // Puede ser nulo
        [Column("codigo")]
        public string? Codigo { get; set; }
        [JsonIgnore]
        public List<Asistencia>? AsistenciasCreadas { get; set; }
        [JsonIgnore]
        public List<Asistencia>? AsistenciasModificadas { get; set; }
        [JsonIgnore]
        public List<Especialidad>? EspecialidadsCreadas { get; set; }
        [JsonIgnore]
        public List<Especialidad>? EspecialidadsModificadas { get; set; }
        [JsonIgnore]
        public List<Formula>? FormulasCreadas { get; set; }
        [JsonIgnore]
        public List<Formula>? FormulasModificadas { get; set; }
        [JsonIgnore]
        public List<Medico>? MedicosCreadas { get; set; }
        [JsonIgnore]
        public List<Medico>? MedicosModificadas { get; set; }
        [JsonIgnore]
        public List<Paciente>? PacientesCreadas { get; set; }
        [JsonIgnore]
        public List<Paciente>? PacientesModificadas { get; set; }
        [JsonIgnore]
        public List<Pedido>? PedidosCreadas { get; set; }
        [JsonIgnore]
        public List<Pedido>? PedidosModificadas { get; set; }
        [JsonIgnore]
        public List<Persona>? PersonasCreadas { get; set; }
        [JsonIgnore]
        public List<Persona>? PersonasModificadas { get; set; }
        [JsonIgnore]
        public List<Sede>? SedesCreadas { get; set; }
        [JsonIgnore]
        public List<Sede>? SedesModificadas { get; set; }
        [JsonIgnore]
        public List<Usuario>? UsuariosCreadas { get; set; }
        [JsonIgnore]
        public List<Usuario>? UsuariosModificadas { get; set; }
        [JsonIgnore]
        public List<TipoUsuario>? TUCreadas { get; set; }
        [JsonIgnore]
        public List<TipoUsuario>? TUModificadas { get; set; }
        [JsonIgnore]
        public List<ProdTerm>? PTCreados { get; set; }
        [JsonIgnore]
        public List<ProdTerm>? PTModificados { get; set; }
        [JsonIgnore]
        public List<Cobro>? CobrosCreadas { get; set; }
        [JsonIgnore]
        public List<Cobro>? CobrosModificadas { get; set; }
        [JsonIgnore]
        public List<Laboratorio>? LaboratorioCreadas { get; set; }
        [JsonIgnore]
        public List<Laboratorio>? LaboratorioModificadas { get; set; }
        [JsonIgnore]
        public List<Laboratorio>? FormulasAutorizadas { get; set; }
        [JsonIgnore]
        public List<Laboratorio>? FormulasElaboradas { get; set; }
        [JsonIgnore]
        public List<Producto>? ProductoCreadas { get; set; }
        [JsonIgnore]
        public List<Producto>? ProductoModificadas { get; set; }
        [JsonIgnore]
        public List<Empaque>? EmpaquesCreadas { get; set; }
        [JsonIgnore]
        public List<Empaque>? EmpaquesModificadas { get; set; }
        [JsonIgnore]
        public List<FormulaCC>? FormulaCCsCreadas { get; set; }
        [JsonIgnore]
        public List<FormulaCC>? FormulaCCsModificadas { get; set; }
        [JsonIgnore]
        public List<Insumo>? InsumosCreadas { get; set; }
        [JsonIgnore]
        public List<Insumo>? InsumosModificadas { get; set; }
        [JsonIgnore]
        public List<InsumoR>? InsumoRsCreadas { get; set; }
        [JsonIgnore]
        public List<InsumoR>? InsumoRsModificadas { get; set; }
        [JsonIgnore]
        public List<FormulaR>? FormulaRsCreadas { get; set; }
        [JsonIgnore]
        public List<FormulaR>? FormulasRsModificadas { get; set; }
    }
    [Table("tipos_usuario")]
    public class TipoUsuario
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int? Id { get; set; }  // Puede ser nulo 
        [Column("nombre")]
        public string? Nombre { get; set; }  // Puede ser nulo
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }  // Puede ser nulo        
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; }  // Puede ser nulo
        [Column("creador_id")]
        public int? CreadorId { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [Column("modificador_id")]
        public int? ModificadorId { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [JsonIgnore]
        public List<Usuario>? Usuarios { get; set; }
    }
}