using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Kardex
{
    /// <summary>Asignación de ubicación y límite de stock por sede.</summary>
    public class KardexUbicacionService : IKardexUbicacionService
    {
        private readonly ApiContext _context;
        private readonly ISupplyRepository _repository;
        private readonly IUnitOfWork _unitWork;

        public KardexUbicacionService(ApiContext context, ISupplyRepository repository, IUnitOfWork unitWork)
        {
            _context = context;
            _repository = repository;
            _unitWork = unitWork;
        }

        public async Task<object> AssignLocation(AssignLocationReq request)
        {
            string fam = (request.Familia ?? "MP").ToUpper().Trim();

            if (fam == "ME")
            {
                var empaqueSede = await _context.EmpaqueSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEmpaque == request.IdInsumo);

                if (empaqueSede == null)
                {
                    empaqueSede = new EmpaqueSede
                    {
                        IdSite = request.IdSede,
                        IdEmpaque = request.IdInsumo,
                        Location = request.Ubicacion
                    };
                    await _context.EmpaqueSupplies.AddAsync(empaqueSede);
                }
                else
                {
                    empaqueSede.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return empaqueSede;
            }
            else if (fam == "PT")
            {
                var productoSede = await _context.ProductSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdProducto == request.IdInsumo);

                if (productoSede == null)
                {
                    productoSede = new ProductoSede
                    {
                        IdSite = request.IdSede,
                        IdProducto = request.IdInsumo,
                        Location = request.Ubicacion
                    };
                    await _context.ProductSupplies.AddAsync(productoSede);
                }
                else
                {
                    productoSede.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return productoSede;
            }
            else if (fam == "ECO")
            {
                var economatoSede = await _context.EconomatoSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEconomato == request.IdInsumo);

                if (economatoSede == null)
                {
                    economatoSede = new EconomatoSede
                    {
                        IdSite = request.IdSede,
                        IdEconomato = request.IdInsumo,
                        Location = request.Ubicacion
                    };
                    await _context.EconomatoSupplies.AddAsync(economatoSede);
                }
                else
                {
                    economatoSede.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return economatoSede;
            }
            else
            {
                // Default a MP / PI (SiteSupply)
                SiteSupply? siteSupply = await _repository.GetSedeSupplyAsync(request.IdInsumo, request.IdSede);

                if (siteSupply == null)
                {
                    siteSupply = new SiteSupply
                    {
                        IdSite = request.IdSede,
                        IdSupply = request.IdInsumo,
                        Location = request.Ubicacion
                    };

                    siteSupply = await _repository.CreateLocationBySiteAsync(siteSupply);
                }
                else
                {
                    siteSupply.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return siteSupply;
            }
        }

        public async Task<object> AssignLimite(AssignLimiteReq request)
        {
            string fam = (request.Familia ?? "MP").ToUpper().Trim();

            if (fam == "ME")
            {
                var empaqueSede = await _context.EmpaqueSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEmpaque == request.IdInsumo);

                if (empaqueSede == null)
                {
                    empaqueSede = new EmpaqueSede
                    {
                        IdSite = request.IdSede,
                        IdEmpaque = request.IdInsumo,
                        Limite = request.Limite
                    };
                    await _context.EmpaqueSupplies.AddAsync(empaqueSede);
                }
                else
                {
                    empaqueSede.Limite = request.Limite;
                    empaqueSede.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return empaqueSede;
            }
            else if (fam == "PT")
            {
                var productoSede = await _context.ProductSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdProducto == request.IdInsumo);

                if (productoSede == null)
                {
                    productoSede = new ProductoSede
                    {
                        IdSite = request.IdSede,
                        IdProducto = request.IdInsumo,
                        Limite = request.Limite
                    };
                    await _context.ProductSupplies.AddAsync(productoSede);
                }
                else
                {
                    productoSede.Limite = request.Limite;
                    productoSede.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return productoSede;
            }
            else if (fam == "ECO")
            {
                var economatoSede = await _context.EconomatoSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEconomato == request.IdInsumo);

                if (economatoSede == null)
                {
                    economatoSede = new EconomatoSede
                    {
                        IdSite = request.IdSede,
                        IdEconomato = request.IdInsumo,
                        Limite = request.Limite
                    };
                    await _context.EconomatoSupplies.AddAsync(economatoSede);
                }
                else
                {
                    economatoSede.Limite = request.Limite;
                    economatoSede.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return economatoSede;
            }
            else
            {
                // Default a MP / PI (SiteSupply)
                SiteSupply? siteSupply = await _repository.GetSedeSupplyAsync(request.IdInsumo, request.IdSede);

                if (siteSupply == null)
                {
                    siteSupply = new SiteSupply
                    {
                        IdSite = request.IdSede,
                        IdSupply = request.IdInsumo,
                        Limite = request.Limite
                    };

                    siteSupply = await _repository.CreateLocationBySiteAsync(siteSupply);
                }
                else
                {
                    siteSupply.Limite = request.Limite;
                    siteSupply.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return siteSupply;
            }
        }
    }
}
