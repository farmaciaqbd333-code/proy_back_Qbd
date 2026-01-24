using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Data
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options)
        {
        }
        // DbSets actualizados a las clases correctas
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<Sede> Sedes { get; set; }  // Para la tabla de secciones
        public DbSet<Persona> Personas { get; set; }  // Para la tabla de secciones
        public DbSet<Usuario> Usuarios { get; set; }  // Para la tabla de secciones
        public DbSet<Paciente> Pacientes { get; set; }  // Para la tabla de secciones
        public DbSet<Pedido> Pedidos { get; set; }  // Para la tabla de secciones
        public DbSet<Medico> Medicos { get; set; }  // Para la tabla de secciones
        public DbSet<Formula> Formulas { get; set; }  // Para la tabla de secciones
        public DbSet<ProdTerm> ProdTerms { get; set; }  // Para la tabla de secciones
        public DbSet<Producto> Productos { get; set; }  // Para la tabla de secciones
        public DbSet<Cobro> Cobros { get; set; }  // Para la tabla de secciones        
        public DbSet<Laboratorio> Laboratorios { get; set; }  // Para la tabla de lab        
        public DbSet<Especialidad> Especialidads { get; set; }  // Para la tabla de lab        
        public DbSet<Insumo> Insumos { get; set; }  // Para la tabla de lab        
        public DbSet<FormulaCC> FormulasCC { get; set; }  // Para la tabla de lab        
        public DbSet<Empaque> Empaques { get; set; }
        public DbSet<FormulaR> FormulasR { get; set; }
        public DbSet<InsumoR> InsumosR { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureAsistencia(modelBuilder);
            ConfigureEspecialidad(modelBuilder);
            ConfigureFormula(modelBuilder);
            ConfigureMedico(modelBuilder);
            ConfigurePaciente(modelBuilder);
            ConfigurePedido(modelBuilder);
            ConfigurePersona(modelBuilder);
            ConfigureSede(modelBuilder);
            ConfigureUsuario(modelBuilder);
            ConfigureTipoUsuario(modelBuilder);
            ConfigureProductosTerminados(modelBuilder);
            ConfigureProductos(modelBuilder);
            ConfigureCobros(modelBuilder);
            ConfigureLaboratorio(modelBuilder);
            ConfigureEmpaque(modelBuilder);
            ConfigureFormulasCC(modelBuilder);
            ConfigureInsumo(modelBuilder);
            ConfigureInsumoR(modelBuilder);
            ConfigureFormulaR(modelBuilder);
        }

        private void ConfigureInsumoR(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InsumoR>()
            .HasKey(fi => new { fi.FormulaRId, fi.InsumoId }); // Clave compuesta
            modelBuilder.Entity<InsumoR>()
                           .HasOne(e => e.Creador)
                           .WithMany(e2 => e2.InsumoRsCreadas)
                           .HasForeignKey(e => e.CreadorId)
                           .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InsumoR>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.InsumoRsModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InsumoR>()
                            .Property(p => p.FechaCreacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
            modelBuilder.Entity<InsumoR>()
                            .Property(p => p.FechaModificacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
        }

        private void ConfigureFormulaR(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FormulaR>()
                           .HasOne(e => e.Creador)
                           .WithMany(e2 => e2.FormulaRsCreadas)
                           .HasForeignKey(e => e.CreadorId)
                           .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FormulaR>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.FormulasRsModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FormulaR>()
                            .Property(p => p.FechaCreacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
            modelBuilder.Entity<FormulaR>()
                            .Property(p => p.FechaModificacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
        }

        private void ConfigureInsumo(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Insumo>()
                           .HasOne(e => e.Creador)
                           .WithMany(e2 => e2.InsumosCreadas)
                           .HasForeignKey(e => e.CreadorId)
                           .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Insumo>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.InsumosModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Insumo>()
                            .Property(p => p.FechaCreacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Insumo>()
                            .Property(p => p.FechaModificacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
        }

        private void ConfigureFormulasCC(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FormulaCC>()
            .HasKey(fi => new { fi.FormulaId, fi.InsumoId, fi.Variable, fi.SedeId }); // Clave compuesta
            modelBuilder.Entity<FormulaCC>()
                .HasOne(e => e.Formula)
                .WithMany(e2 => e2.FormulaCC)
                .HasForeignKey(e => new { e.FormulaId, e.SedeId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FormulaCC>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.FormulaCCsCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FormulaCC>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.FormulaCCsModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FormulaCC>()
                            .Property(p => p.FechaCreacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
            modelBuilder.Entity<FormulaCC>()
                            .Property(p => p.FechaModificacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
        }

        private void ConfigureEmpaque(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empaque>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.EmpaquesCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Empaque>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.EmpaquesModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Empaque>()
                            .Property(p => p.FechaCreacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Empaque>()
                            .Property(p => p.FechaModificacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
        }

        private void ConfigureAsistencia(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asistencia>()
            .HasKey(fi => new { fi.Id, fi.SedeId }); // Clave compuesta
            modelBuilder.Entity<Asistencia>()
                .HasOne(e => e.Sede)
                .WithMany(e2 => e2.Asistencias)
                .HasForeignKey(e => new { e.SedeId })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Asistencia>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.AsistenciasCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Asistencia>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.AsistenciasModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Asistencia>()
                            .Property(p => p.FechaCreacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Asistencia>()
                            .Property(p => p.FechaModificacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
        }
        private void ConfigureCobros(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cobro>()
            .HasKey(fi => new { fi.Id, fi.SedeId }); // Clave compuesta
            modelBuilder.Entity<Cobro>()
                            .HasOne(e => e.Pedido)
                            .WithMany(e2 => e2.Cobros)
                            .HasForeignKey(e => new { e.PedidoId, e.SedeId })
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Cobro>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.CobrosCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Cobro>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.CobrosModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Cobro>()
                            .Property(p => p.FechaCreacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Cobro>()
                            .Property(p => p.FechaModificacion)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
                            .ValueGeneratedOnAdd();
        }
        private void ConfigureEspecialidad(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Especialidad>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.EspecialidadsCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Especialidad>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.EspecialidadsModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Especialidad>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Especialidad>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureFormula(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Formula>()
            .HasKey(fi => new { fi.Id, fi.SedeId }); // Clave compuesta
            modelBuilder.Entity<Formula>()
                            .HasOne(e => e.Pedido)
                            .WithMany(e2 => e2.Formulas)
                            .HasForeignKey(e => new { e.PedidoId, e.SedeId })
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Formula>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.FormulasCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Formula>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.FormulasModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Formula>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Formula>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureLaboratorio(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Laboratorio>()
            .HasKey(fi => new { fi.Id, fi.SedeId });
            modelBuilder.Entity<Laboratorio>()
                .HasOne(e => e.Formula)
                .WithOne(e2 => e2.Laboratorio)
                .HasForeignKey<Laboratorio>(e => new { e.Id, e.SedeId }) // Assuming `FormulaId` is the FK
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratorio>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.LaboratorioCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratorio>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.LaboratorioModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratorio>()
                            .HasOne(e => e.AutorizadoU)
                            .WithMany(e2 => e2.FormulasAutorizadas)
                            .HasForeignKey(e => e.Autorizado)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratorio>()
                            .HasOne(e => e.ElaboradoU)
                            .WithMany(e2 => e2.FormulasElaboradas)
                            .HasForeignKey(e => e.Elaborado)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratorio>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Laboratorio>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureMedico(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Medico>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.MedicosCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Medico>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.MedicosModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Medico>()
                            .HasOne(e => e.Persona)
                            .WithMany(e2 => e2.Medicos)
                            .HasForeignKey(e => e.PersonaId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Medico>()
                            .HasOne(e => e.Especialidad)
                            .WithMany(e2 => e2.Medicos)
                            .HasForeignKey(e => e.EspecialidadId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Medico>()
                            .HasOne(e => e.Sede)
                            .WithMany(e2 => e2.Medicos)
                            .HasForeignKey(e => e.SedeId)                            ;
            modelBuilder.Entity<Medico>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Medico>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }

        private void ConfigurePaciente(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Paciente>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.PacientesCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Paciente>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.PacientesModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Paciente>()
                            .HasOne(e => e.Persona)
                            .WithMany(e2 => e2.Pacientes)
                            .HasForeignKey(e => e.PersonaId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Paciente>()
                            .HasOne(e => e.Sede)
                            .WithMany(e2 => e2.Pacientes)
                            .HasForeignKey(e => e.SedeId);
            modelBuilder.Entity<Paciente>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Paciente>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigurePedido(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pedido>()
            .HasKey(fi => new { fi.Id, fi.SedeId });
            modelBuilder.Entity<Pedido>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.PedidosCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Pedido>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.PedidosModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Pedido>()
                            .HasOne(e => e.Medico)
                            .WithMany(e2 => e2.Pedidos)
                            .HasForeignKey(e => e.MedicoId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Pedido>()
                            .HasOne(e => e.Paciente)
                            .WithMany(e2 => e2.Pedidos)
                            .HasForeignKey(e => e.PacienteId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Pedido>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Pedido>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigurePersona(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Persona>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.PersonasCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Persona>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.PersonasModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Persona>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Persona>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureProductosTerminados(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProdTerm>()
            .HasKey(fi => new { fi.Id, fi.SedeId }); // Clave compuesta
            modelBuilder.Entity<ProdTerm>()
                            .HasOne(e => e.Pedido)
                            .WithMany(e2 => e2.ProdTerms)
                            .HasForeignKey(e => new { e.PedidoId, e.SedeId })
                            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProdTerm>()
                            .HasOne(e => e.Producto)
                            .WithMany(e2 => e2.ProdTerm)
                            .HasForeignKey(hfk => hfk.ProductoId)
                            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProdTerm>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.PTCreados)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProdTerm>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.PTModificados)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProdTerm>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<ProdTerm>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureProductos(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Producto>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.ProductoCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Producto>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.ProductoModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Producto>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Producto>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureSede(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sede>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.SedesCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Sede>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.SedesModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            // modelBuilder.Entity<Sede>()
            //                 .HasOne(e => e.Encargado)
            //                 .WithOne(e2 => e2.Sede)
            //                 .HasForeignKey<Usuario>(e => e.Sede)
            //                 .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Sede>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Sede>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.UsuariosCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Usuario>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.UsuariosModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Usuario>()
                            .HasOne(e => e.Persona)
                            .WithMany(e2 => e2.Usuarios)
                            .HasForeignKey(e => e.PersonaId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Usuario>()
                            .HasOne(e => e.Tipo)
                            .WithMany(e2 => e2.Usuarios)
                            .HasForeignKey(e => e.TipoId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Usuario>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<Usuario>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
        private void ConfigureTipoUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TipoUsuario>()
                            .HasOne(e => e.Creador)
                            .WithMany(e2 => e2.TUCreadas)
                            .HasForeignKey(e => e.CreadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TipoUsuario>()
                            .HasOne(e => e.Modificador)
                            .WithMany(e2 => e2.TUModificadas)
                            .HasForeignKey(e => e.ModificadorId)
                            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TipoUsuario>()
            .Property(p => p.FechaCreacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<TipoUsuario>()
            .Property(p => p.FechaModificacion)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")  // Para asignar el valor al insertar
            .ValueGeneratedOnAdd();
        }
    }

}