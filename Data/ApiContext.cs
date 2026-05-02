using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.ElaboracionBase;
using proy_back_Qbd.Models.NotaSalida;
using proy_back_Qbd.Models.Paquete;
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
        public DbSet<ElaboracionBase> ElaboracionBases { get; set; }
        public DbSet<PaqueteSa> PaqueteSas { get; set; }
        public DbSet<Paquete> Paquetes { get; set; }
        public DbSet<DetalleNotaSalida> DetalleNotaSalidas { get; set; }
        public DbSet<NotaSalida> NotaSalidas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<DetalleCompra> DetalleCompras { get; set; }
        public DbSet<Compra> Compras { get; set; }
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
        public DbSet<Familia> Familias { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            ConfigureNotaSalida(modelBuilder);
        }

        private void ConfigureNotaSalida(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NotaSalida>((e) =>
           {
               e.HasOne(ho => ho.Creador).WithMany(wm => wm.NotaSalidaCreadas).HasForeignKey(hfk => hfk.IdCreador);
               e.HasOne(ho => ho.Modificador).WithMany(wm => wm.NotaSalidaModificadas).HasForeignKey(hfk => hfk.IdModificador);
           });

            modelBuilder.Entity<Proveedor>((e) =>
           {
               e.HasOne(ho => ho.Creador).WithMany(wm => wm.ProveedoresCreados).HasForeignKey(hfk => hfk.IdCreador);
               e.HasOne(ho => ho.Modificador).WithMany(wm => wm.ProveedoresModificados).HasForeignKey(hfk => hfk.IdModificador);
           });

            modelBuilder.Entity<ElaboracionBase>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.ElaboracionBaseCreados).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.ElaboracionBaseModificados).HasForeignKey(hfk => hfk.IdModificador);
            });
            modelBuilder.Entity<PaqueteSa>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.PaquetesSACreados).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.PaquetesSAModificados).HasForeignKey(hfk => hfk.IdModificador);
            });
            modelBuilder.Entity<Paquete>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.PaquetesCreados).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.PaquetesModificados).HasForeignKey(hfk => hfk.IdModificador);
            });
            modelBuilder.Entity<DetalleNotaSalida>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleNotaSalidaCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleNotaSalidaModificadas).HasForeignKey(hfk => hfk.IdModificador);
            });
            modelBuilder.Entity<DetalleCompra>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleComprasCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleComprasModificadas).HasForeignKey(hfk => hfk.IdModificador);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Compra>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wo => wo.ComprasCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wo => wo.ComprasModificadas).HasForeignKey(hfk => hfk.IdModificador);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<InsumoR>((e) =>
            {
                e.HasKey(fi => new { fi.FormulaRId, fi.InsumoId }); // Clave compuesta
                e.HasOne(e => e.Creador).WithMany(e2 => e2.InsumoRsCreadas).HasForeignKey(e => e.CreadorId);
                e.HasOne(e => e.Modificador).WithMany(e2 => e2.InsumoRsModificadas).HasForeignKey(e => e.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<FormulaR>((e) =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.FormulaRsCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.FormulasRsModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Insumo>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.InsumosCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.InsumosModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<FormulaCC>(e =>
            {
                e.HasKey(x => new { x.FormulaId, x.InsumoId, x.Variable, x.SedeId });
                e.HasOne(x => x.Formula).WithMany(x => x.FormulaCC).HasForeignKey(x => new { x.FormulaId, x.SedeId });
                e.HasOne(x => x.Creador).WithMany(x => x.FormulaCCsCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.FormulaCCsModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Empaque>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.EmpaquesCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.EmpaquesModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Asistencia>(e =>
            {
                e.HasKey(x => new { x.Id, x.SedeId });
                e.HasOne(x => x.Sede).WithMany(x => x.Asistencias).HasForeignKey(x => x.SedeId);
                e.HasOne(x => x.Creador).WithMany(x => x.AsistenciasCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.AsistenciasModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<Cobro>(e =>
            {
                e.HasKey(x => new { x.Id, x.SedeId });
                e.HasOne(x => x.Pedido).WithMany(x => x.Cobros).HasForeignKey(x => new { x.PedidoId, x.SedeId });
                e.HasOne(x => x.Creador).WithMany(x => x.CobrosCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.CobrosModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<Especialidad>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.EspecialidadsCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.EspecialidadsModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<Formula>(e =>
            {
                e.HasKey(x => new { x.Id, x.SedeId });
                e.HasOne(x => x.Pedido).WithMany(x => x.Formulas).HasForeignKey(x => new { x.PedidoId, x.SedeId });
                e.HasOne(x => x.Creador).WithMany(x => x.FormulasCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.FormulasModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Laboratorio>(e =>
            {
                e.HasKey(x => new { x.Id, x.SedeId });
                e.HasOne(x => x.Formula).WithOne(x => x.Laboratorio).HasForeignKey<Laboratorio>(x => new { x.Id, x.SedeId });
                e.HasOne(x => x.Creador).WithMany(x => x.LaboratorioCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.LaboratorioModificadas).HasForeignKey(x => x.ModificadorId);
                e.HasOne(x => x.AutorizadoU).WithMany(x => x.FormulasAutorizadas).HasForeignKey(x => x.Autorizado);
                e.HasOne(x => x.ElaboradoU).WithMany(x => x.FormulasElaboradas).HasForeignKey(x => x.Elaborado);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Medico>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.MedicosCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.MedicosModificadas).HasForeignKey(x => x.ModificadorId);
                e.HasOne(x => x.Persona).WithMany(x => x.Medicos).HasForeignKey(x => x.PersonaId);
                e.HasOne(x => x.Especialidad).WithMany(x => x.Medicos).HasForeignKey(x => x.EspecialidadId);
                e.HasOne(x => x.Sede).WithMany(x => x.Medicos).HasForeignKey(x => x.SedeId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Paciente>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.PacientesCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.PacientesModificadas).HasForeignKey(x => x.ModificadorId);
                e.HasOne(x => x.Persona).WithMany(x => x.Pacientes).HasForeignKey(x => x.PersonaId);
                e.HasOne(x => x.Sede).WithMany(x => x.Pacientes).HasForeignKey(x => x.SedeId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Pedido>(e =>
            {
                e.HasKey(x => new { x.Id, x.SedeId });
                e.HasOne(x => x.Creador).WithMany(x => x.PedidosCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.PedidosModificadas).HasForeignKey(x => x.ModificadorId);
                e.HasOne(x => x.Medico).WithMany(x => x.Pedidos).HasForeignKey(x => x.MedicoId);
                e.HasOne(x => x.Paciente).WithMany(x => x.Pedidos).HasForeignKey(x => x.PacienteId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Persona>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.PersonasCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.PersonasModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<ProdTerm>(e =>
            {
                e.HasKey(x => new { x.Id, x.SedeId });
                e.HasOne(x => x.Pedido).WithMany(x => x.ProdTerms).HasForeignKey(x => new { x.PedidoId, x.SedeId });
                e.HasOne(x => x.Producto).WithMany(x => x.ProdTerm).HasForeignKey(x => x.ProductoId);
                e.HasOne(x => x.Creador).WithMany(x => x.PTCreados).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.PTModificados).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Producto>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.ProductoCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.ProductoModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Sede>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.SedesCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.SedesModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });


            modelBuilder.Entity<Usuario>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.UsuariosCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.UsuariosModificadas).HasForeignKey(x => x.ModificadorId);
                e.HasOne(x => x.Persona).WithMany(x => x.Usuarios).HasForeignKey(x => x.PersonaId);
                e.HasOne(x => x.Tipo).WithMany(x => x.Usuarios).HasForeignKey(x => x.TipoId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<TipoUsuario>(e =>
            {
                e.HasOne(x => x.Creador).WithMany(x => x.TUCreadas).HasForeignKey(x => x.CreadorId);
                e.HasOne(x => x.Modificador).WithMany(x => x.TUModificadas).HasForeignKey(x => x.ModificadorId);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<Familia>((e) =>
            {
                e.HasKey(hk => hk.Id);
                e.HasOne(ho => ho.Creador).WithMany().HasForeignKey(hfk => hfk.IdCreador);
            });
        }
    }
}