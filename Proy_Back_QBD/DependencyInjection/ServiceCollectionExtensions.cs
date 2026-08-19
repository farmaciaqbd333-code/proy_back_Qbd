using Microsoft.Extensions.DependencyInjection;
using Proy_back_QBD.Services;
using Proy_back_QBD.Services.Interfaces;
using proy_back_Qbd.Services;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Service.AjusteService;
using Proy_back_QBD.Interface;
using proy_back_Qbd.Services.Interfaces.INotaSalidaService;
using Proy_back_QBD.Services.NotaSalidaService;
using proy_back_Qbd.Repositories.Interfaces;
using proy_back_Qbd.Repositories;

namespace Proy_back_QBD.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IKardexService, KardexService>();
            services.AddScoped<ISedeService, SedeService>();
            services.AddScoped<IAsistenciaService, AsistenciaService>();
            services.AddScoped<IPacienteService, PacienteService>();
            services.AddScoped<IMedicoService, MedicoService>();
            services.AddScoped<IPedidoService, PedidoService>();
            services.AddScoped<IFormulaService, FormulaService>();
            services.AddScoped<IFormulaRService, FormulaRService>();
            services.AddScoped<IFormulaCCService, FormulaCCService>();
            services.AddScoped<IInsumoRService, InsumoRService>();
            services.AddScoped<IInsumoService, InsumoService>();
            services.AddScoped<IProdTermService, ProdTermService>();
            services.AddScoped<IProductoService, ProductoService>();
            services.AddScoped<ILaboratorioService, LaboratorioService>();
            services.AddScoped<IEspecialidadService, EspecialidadService>();
            services.AddScoped<ICobroService, CobroService>();
            services.AddScoped<ICajaService, CajaService>();
            services.AddScoped<IEmpaqueService, EmpaqueService>();
            services.AddScoped<IOrdenCompraService, OrdenCompraService>();
            services.AddScoped<IEconomatoService, EconomatoService>();
            services.AddScoped<ICompraLaboratorioService, CompraLaboratorioService>();
            services.AddScoped<IPaqueteService, PaqueteService>();
            services.AddScoped<IMesonService, MesonService>();
            services.AddScoped<IAjusteService, AjusteService>();
            services.AddScoped<IProductoIntermedioService, ProductoIntermedioService>();
            services.AddScoped<INotaSalidaService, NotaSalidaService>();
            services.AddScoped<IRecepcionRepository, RecepcionRepository>();
            services.AddScoped<IRecepcionService, RecepcionService>();

            return services;
        }
    }
}
