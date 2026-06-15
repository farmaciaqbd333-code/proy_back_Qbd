using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.ElaboracionBase;
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
        public DbSet<ElaboracionBase> ElaboracionBases { get; set; }
        public DbSet<AjusteEmpaque> AjusteEmpaques { get; set; }
        public DbSet<AjusteInsumo> AjusteInsumos { get; set; }
        public DbSet<PaqueteSa> PaqueteSas { get; set; }
        public DbSet<Paquete> Paquetes { get; set; }
        public DbSet<PaqueteInsumo> PaqueteInsumos { get; set; }
        public DbSet<PaqueteEmpaque> PaqueteEmpaques { get; set; }
        public DbSet<DetalleNotaSalidaInsumo> DetalleNotaSalidaInsumo { get; set; }
        public DbSet<DetalleNotaSalidaEmpaque> DetalleNotaSalidaEmpaques { get; set; }
        public DbSet<NotaSalida> NotaSalidas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<CompraInsumos> CompraInsumos { get; set; }
        public DbSet<CompraOtros> CompraOtros { get; set; }
        public DbSet<CompraEconomatos> CompraEconomatos { get; set; }
        public DbSet<CompraEmpaques> CompraEmpaques { get; set; }
        public DbSet<CompraProductos> CompraProductos { get; set; }
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
        public DbSet<Insumo> Insumos { get; set; }
        public DbSet<Economato> Economatos { get; set; }
        public DbSet<FormulaCC> FormulasCC { get; set; }  // Para la tabla de lab        
        public DbSet<Empaque> Empaques { get; set; }
        public DbSet<FormulaR> FormulasR { get; set; }
        public DbSet<InsumoR> InsumosR { get; set; }
        public DbSet<Familia> Familias { get; set; }
        public DbSet<Fabricante> Fabricantes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            Configure(modelBuilder);
        }

        private void Configure(ModelBuilder modelBuilder)
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
               e.Property(p => p.Id).ValueGeneratedOnAdd();
               e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
               e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
           });

            modelBuilder.Entity<ElaboracionBase>((e) =>
            {
                e.HasOne(ho => ho.Empaque).WithMany(wm => wm.ElaboracionBases).HasForeignKey(hfk => hfk.IdEmpaque);
                e.HasOne(ho => ho.Insumo).WithMany(wm => wm.ElaboracionBases).HasForeignKey(hfk => hfk.IdInsumo);
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
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
            });
            modelBuilder.Entity<PaqueteEmpaque>((e) =>
            {
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasOne(p => p.Paquete).WithOne(w => w.PaqueteEmpaques).HasForeignKey<PaqueteEmpaque>(h => h.IdPaquete);
                e.HasOne(p => p.CompraEmpaques).WithMany(w => w.PaqueteEmpaques).HasForeignKey(h => h.IdCompraEmpaque);
            });
            modelBuilder.Entity<PaqueteInsumo>((e) =>
            {
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasOne(p => p.Paquete).WithOne(w => w.PaqueteInsumos).HasForeignKey<PaqueteInsumo>(h => h.IdPaquete);
                e.HasOne(p => p.CompraInsumos).WithMany(w => w.PaqueteInsumos).HasForeignKey(h => h.IdCompraInsumo);
            });
            modelBuilder.Entity<DetalleNotaSalidaInsumo>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleNotaSalidaCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleNotaSalidaModificadas).HasForeignKey(hfk => hfk.IdModificador);
            });
            modelBuilder.Entity<CompraOtros>((e) =>
            {
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasOne(ho => ho.Compra).WithMany(wm => wm.CompraOtros).HasForeignKey(hfk => hfk.IdCompra);
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleComprasCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleComprasModificadas).HasForeignKey(hfk => hfk.IdModificador);
                e.HasOne(ho => ho.Familia).WithMany(wm => wm.CompraOtros).HasForeignKey(hfk => hfk.IdFamilia);
            });
            modelBuilder.Entity<CompraInsumos>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleComprasInsumoCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleCompraInsumosModificadas).HasForeignKey(hfk => hfk.IdModificador);
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
                e.HasOne(ho => ho.Compra).WithMany(wm => wm.CompraInsumos).HasForeignKey(hfk => hfk.IdCompra).IsRequired(false);
                e.HasOne(ho => ho.Insumo).WithMany(wm => wm.CompraInsumos).HasForeignKey(hfk => hfk.IdInsumo).IsRequired(false);
                e.HasOne(ho => ho.Fabricante).WithMany(wm => wm.DetalleCompras).HasForeignKey(hfk => hfk.IdFabricante);
            });
            modelBuilder.Entity<CompraEconomatos>((e) =>
            {
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasOne(ho => ho.Compra).WithMany(wm => wm.CompraEconomatos).HasForeignKey(hfk => hfk.IdCompra).IsRequired(false);
                e.HasOne(ho => ho.Economato).WithMany(wm => wm.DetalleCompraEconomatos).HasForeignKey(hfk => hfk.IdEconomato).IsRequired(false);
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleCompraEconomatoCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleCompraEconomatoModificadas).HasForeignKey(hfk => hfk.IdModificador);
            });
            modelBuilder.Entity<Economato>((e) =>
            {
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasOne(x => x.Familia).WithMany(x => x.Economatos).HasForeignKey(x => x.IdFamilia);
            });
            modelBuilder.Entity<CompraEmpaques>((e) =>
            {
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasOne(ho => ho.Compra).WithMany(wm => wm.CompraEmpaques).HasForeignKey(hfk => hfk.IdCompra).IsRequired(false);
                e.HasOne(ho => ho.Empaque).WithMany(wm => wm.CompraEmpaques).HasForeignKey(hfk => hfk.IdEmpaque).IsRequired(false);
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleComprasEmpaquesCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleComprasEmpaquesModificadas).HasForeignKey(hfk => hfk.IdModificador);
                e.HasOne(ho => ho.Fabricante).WithMany(wm => wm.DetalleCompraEmpaques).HasForeignKey(hfk => hfk.IdFabricante);
            });
            modelBuilder.Entity<CompraProductos>((e) =>
            {
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasOne(ho => ho.Compra).WithMany(wm => wm.CompraProductos).HasForeignKey(hfk => hfk.IdCompra).IsRequired(false);
                e.HasOne(ho => ho.Producto).WithMany(wm => wm.DetalleCompraProductos).HasForeignKey(hfk => hfk.IdProducto).IsRequired(false);
                e.HasOne(ho => ho.Creador).WithMany(wm => wm.DetalleCompraProductoCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wm => wm.DetalleCompraProductoModificadas).HasForeignKey(hfk => hfk.IdModificador);
            });
            modelBuilder.Entity<Compra>((e) =>
            {
                e.HasOne(ho => ho.Creador).WithMany(wo => wo.ComprasCreadas).HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany(wo => wo.ComprasModificadas).HasForeignKey(hfk => hfk.IdModificador);
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
                e.HasOne(ho => ho.Proveedor).WithMany(wo => wo.Compras).HasForeignKey(hfk => hfk.IdProveedor);
                e.HasOne(ho => ho.Sede).WithMany(wo => wo.Compras).HasForeignKey(hfk => hfk.IdSede);
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
                e.HasOne(x => x.Familia).WithMany(x => x.Insumos).HasForeignKey(x => x.IdFamilia);
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
                e.HasOne(x => x.Caja).WithMany(x => x.ListaCajas).HasForeignKey(x => x.IdCaja);
                e.HasOne(x => x.Funda).WithMany(x => x.ListaFundas).HasForeignKey(x => x.IdFunda);
                e.HasOne(x => x.Etiqueta1).WithMany(x => x.ListaEtiquetas1).HasForeignKey(x => x.IdEtiqueta1);
                e.HasOne(x => x.Etiqueta2).WithMany(x => x.ListaEtiquetas2).HasForeignKey(x => x.IdEtiqueta2);
                e.HasOne(x => x.Familia).WithMany(x => x.Empaques).HasForeignKey(x => x.IdFamilia);
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
                e.HasOne(x => x.Familia).WithMany(x => x.Productos).HasForeignKey(x => x.IdFamilia);
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
                e.HasOne(ho => ho.Creador).WithMany().HasForeignKey(hfk => hfk.IdCreador);
            });

            modelBuilder.Entity<Fabricante>((e) =>
            {
                e.HasKey(hk => hk.Id);
                e.HasOne(ho => ho.Creador).WithMany().HasForeignKey(hfk => hfk.IdCreador);
                e.HasOne(ho => ho.Modificador).WithMany().HasForeignKey(hfk => hfk.IdModificador);
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.Property(p => p.FechaCreacion).ValueGeneratedOnAdd();
                e.Property(p => p.FechaModificacion).ValueGeneratedOnAddOrUpdate();
                e.HasMany(hm => hm.Proveedores)
                    .WithMany(wm => wm.Fabricantes)
                    .UsingEntity<Dictionary<string, object>>(
                        "fabricantes_proveedores",
                        j => j.HasOne<Proveedor>().WithMany().HasForeignKey("id_proveedor"),
                        j => j.HasOne<Fabricante>().WithMany().HasForeignKey("id_fabricante")
                    );
            });

        }
    }
}