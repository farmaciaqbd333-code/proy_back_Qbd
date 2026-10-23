using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models.ProductoIntermedio;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    public partial class NotaSalidaService
    {
        private async Task CrearDetalleInsumo(
            int idNotaSalida,
            CreateReq request,
            NotaSalidaFamiliasCreateReq item)
        {
            _logger.LogInformation(
                "Iniciando creación de detalle de insumo/PI. NotaSalida={NotaSalida}, Registro={Registro}, Cantidad={Cantidad}, Um={Um}, Familia={Familia}, SedeOrigen={SedeOrigen}",
                idNotaSalida,
                item.Registro,
                item.Cantidad,
                item.Um,
                item.Familia,
                request.IdSedeOrigen);

            bool isPI = (item.Familia ?? "").Trim().ToUpper() == "PI";
            int sedeOrigen = request.IdSedeOrigen > 0 ? request.IdSedeOrigen : 15;

            ProductoIntermedio? pi = null;
            if (isPI)
            {
                pi = await _context.ProductosIntermedios.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == item.Registro);
            }

            // PI: las mismas filas que muestra el kardex (no cualquier fila con el mismo lote)
            var stocksOrigen = isPI
                ? await ObtenerFilasStockPI(item.Registro, sedeOrigen)
                : new List<StockInsumo>();

            if (!isPI) stocksOrigen = await _context.StockInsumos
                .Include(s => s.ProductoIntermedio)
                .Include(s => s.NotaSalidaInsumo)
                .Where(x =>
                    (isPI ? (x.IdProductoIntermedio == item.Registro || (x.ProductoIntermedio != null && x.ProductoIntermedio.Id == item.Registro) || (x.Tipo == "PI" && pi != null && x.NotaSalidaInsumo != null && x.NotaSalidaInsumo.Lote == pi.Lote)) : x.IdCompraInsumo == item.Registro) &&
                    x.IdSede == sedeOrigen)
                .OrderBy(x => x.IdNotaSalidaInsumo != null ? 1 : 0)
                .ThenByDescending(x => x.StockDisponible)
                .ToListAsync();

            if (!isPI && !stocksOrigen.Any())
            {
                stocksOrigen = await _context.StockInsumos
                    .Include(s => s.ProductoIntermedio)
                    .Include(s => s.NotaSalidaInsumo)
                    .Where(x => isPI ? (x.IdProductoIntermedio == item.Registro || (x.ProductoIntermedio != null && x.ProductoIntermedio.Id == item.Registro) || (x.Tipo == "PI" && pi != null && x.NotaSalidaInsumo != null && x.NotaSalidaInsumo.Lote == pi.Lote)) : x.IdCompraInsumo == item.Registro)
                    .OrderBy(x => x.IdNotaSalidaInsumo != null ? 1 : 0)
                    .ThenByDescending(x => x.StockDisponible)
                    .ToListAsync();
            }

            var stockOrigen = stocksOrigen.FirstOrDefault();

            if (stockOrigen == null)
            {
                if (!isPI)
                {
                    var ci = await _context.CompraInsumos
                        .Include(c => c.Compra)
                        .Include(c => c.NotaSalidaInsumos)
                            .ThenInclude(nsi => nsi.NotaSalida)
                        .FirstOrDefaultAsync(c => c.Id == item.Registro);

                    if (ci != null)
                    {
                        decimal entradas = (ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0)
                            ? ci.CantidadRecibida.Value
                            : ci.CantidadSolicitada;

                        decimal salidasNS = ci.NotaSalidaInsumos
                            .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeOrigen == sedeOrigen)
                            .Sum(nsi => ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * nsi.Cantidad);

                        decimal saldoInicial = Math.Max(0m, entradas - salidasNS);

                        stockOrigen = new StockInsumo
                        {
                            IdCompraInsumo = ci.Id,
                            IdSede = sedeOrigen,
                            Tipo = "MP",
                            StockDisponible = saldoInicial,
                            UnidadMedida = "G"
                        };
                        _context.StockInsumos.Add(stockOrigen);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (stockOrigen == null)
            {
                _logger.LogWarning(
                    "No se encontró stock. Registro={Registro}, SedeOrigen={SedeOrigen}, Familia={Familia}",
                    item.Registro,
                    request.IdSedeOrigen,
                    item.Familia);

                throw new BadRequestException($"No existe stock registrado para el registro {item.Registro} de {item.Familia}.");
            }

            string stockUm = (stockOrigen.UnidadMedida ?? "G").Trim().ToUpper();
            string itemUm = (item.Um ?? "G").Trim().ToUpper();
            decimal cantDescontar = item.Cantidad;

            // Unit conversions between G, KG and UND
            if (!isPI && (itemUm == "KG" || itemUm == "KILOGRAMOS"))
            {
                cantDescontar = item.Cantidad * 1000m;
            }
            else             if ((stockUm == "G" || stockUm == "GR") && (itemUm == "KG" || itemUm == "KILOGRAMOS"))
            {
                cantDescontar = item.Cantidad * 1000m;
            }
            else if ((stockUm == "KG" || stockUm == "KILOGRAMOS") && (itemUm == "G" || itemUm == "GR"))
            {
                cantDescontar = item.Cantidad / 1000m;
            }
            else if (stockUm == "UND" && (itemUm == "KG" || itemUm == "KILOGRAMOS"))
            {
                if (stockOrigen.StockDisponible >= item.Cantidad * 1000m)
                {
                    cantDescontar = item.Cantidad * 1000m;
                }
            }

            else if (stockOrigen.StockDisponible > 0 && stockOrigen.StockDisponible >= item.Cantidad * 1000m && (itemUm == "KG" || itemUm == "KILOGRAMOS"))
            {
                cantDescontar = item.Cantidad * 1000m;
            }

            _logger.LogInformation(
                "Stock encontrado. StockActual={StockActual} {StockUm}, CantidadSolicitada={CantidadSolicitada} {ItemUm}, CantidadDescontar={CantidadDescontar}",
                stockOrigen.StockDisponible,
                stockUm,
                item.Cantidad,
                itemUm,
                cantDescontar);

            decimal totalDisponible = stocksOrigen.Sum(s => s.StockDisponible);

            if (totalDisponible < cantDescontar || totalDisponible <= 0)
            {
                decimal dispFriendly = (stockUm == "G" && (itemUm == "KG" || itemUm == "KILOGRAMOS"))
                    ? Math.Round(totalDisponible / 1000m, 3)
                    : totalDisponible;
                string dispUm = (stockUm == "G" && (itemUm == "KG" || itemUm == "KILOGRAMOS")) ? "KG" : stockUm;

                _logger.LogWarning(
                    "Stock insuficiente. Disponible={Disponible} {StockUm}, Solicitado={Solicitado} {ItemUm} (descuento={CantDescontar})",
                    totalDisponible,
                    stockUm,
                    item.Cantidad,
                    itemUm,
                    cantDescontar);

                throw new BadRequestException($"Stock insuficiente para el registro {item.Registro}. Stock disponible: {dispFriendly} {dispUm}, Solicitado: {item.Cantidad} {itemUm}. No se puede realizar una salida mayor al stock existente.");
            }

            // Si la unidad enviada es KG, convertir a Gramos para guardarlo siempre en G en la base de datos
            decimal cantidadAGuardar = item.Cantidad;
            string umAGuardar = (item.Um ?? "G").Trim().ToUpper();

            if (!isPI && (umAGuardar == "KG" || umAGuardar == "KILOGRAMOS" || umAGuardar == "Kg"))
            {
                cantidadAGuardar = item.Cantidad * 1000m;
                umAGuardar = "G";
            }

            var detalle = new NotaSalidaInsumo
            {
                IdNotaSalida = idNotaSalida,
                IdCompraInsumo = isPI ? null : item.Registro,
                Cantidad = cantidadAGuardar,
                Um = umAGuardar,
                Lote = isPI ? (stockOrigen.ProductoIntermedio?.Lote ?? stockOrigen.IdProductoIntermedio?.ToString() ?? stockOrigen.NotaSalidaInsumo?.Lote ?? pi?.Lote) : null,
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaInsumos = item.Paquetes?.Select(p =>
                {
                    bool esPaqKg = !isPI && ((p.Um ?? "").Trim().ToUpper() == "KG" || (p.Um ?? "").Trim().ToUpper() == "KILOGRAMOS" || (p.Um ?? "").Trim().ToUpper() == "Kg");
                    return new PaqueteNotaSalidaInsumo
                    {
                        IdCreador = request.IdCreador,
                        CantidadPaquete = p.CantidadPaquete,
                        Peso = esPaqKg ? p.Peso * 1000m : p.Peso,
                        Tara = esPaqKg ? p.Tara * 1000m : p.Tara,
                        Um = esPaqKg ? "G" : (p.Um ?? "G"),
                        PesoNeto = esPaqKg ? p.PesoNeto * 1000m : p.PesoNeto,
                        PesoBruto = esPaqKg ? p.PesoBruto * 1000m : p.PesoBruto
                    };
                }).ToList()
            };

            _context.NotaSalidaInsumos.Add(detalle);

            decimal pendienteDescontar = cantDescontar;
            foreach (var st in stocksOrigen)
            {
                if (pendienteDescontar <= 0) break;
                decimal aDescontar = Math.Min(st.StockDisponible, pendienteDescontar);
                st.StockDisponible -= aDescontar;
                pendienteDescontar -= aDescontar;
            }

            _logger.LogInformation(
                "Detalle de NotaSalidaInsumo agregado exitosamente. Registro={Registro}, isPI={isPI}, NuevoStock={NuevoStock}",
                item.Registro, isPI, stockOrigen.StockDisponible);

            _logger.LogInformation(
                "Finalizó creación de detalle de insumo. NotaSalida={NotaSalida}",
                idNotaSalida);
        }

        private async Task CrearDetalleEconomato(
             int idNotaSalida,
             CreateReq request,
             NotaSalidaFamiliasCreateReq item)
        {
            int sedeOrigenEco = request.IdSedeOrigen > 0 ? request.IdSedeOrigen : 15;
            var stocksOrigen = await _context.StockEconomatos
                .Where(x => x.IdCompraEconomato == item.Registro && x.IdSede == sedeOrigenEco)
                .OrderBy(x => x.IdNotaSalidaEconomato != null ? 1 : 0)
                .ThenBy(x => x.Id)
                .ToListAsync();

            if (!stocksOrigen.Any())
            {
                stocksOrigen = await _context.StockEconomatos
                    .Where(x => x.IdCompraEconomato == item.Registro)
                    .OrderBy(x => x.IdNotaSalidaEconomato != null ? 1 : 0)
                    .ThenBy(x => x.Id)
                    .ToListAsync();
            }

            if (!stocksOrigen.Any())
                throw new BadRequestException($"No existe stock registrado para el economato {item.Registro}.");

            decimal totalDisponible = stocksOrigen.Sum(s => s.StockDisponible);
            if (totalDisponible < item.Cantidad || totalDisponible <= 0)
                throw new BadRequestException($"Stock insuficiente para el registro {item.Registro}. Stock disponible: {totalDisponible}, Solicitado: {item.Cantidad}.");

            var detalle = new NotaSalidaEconomato
            {
                IdNotaSalida = idNotaSalida,
                IdCompraEconomato = item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaEconomatos = item.Paquetes?.Select(p => new PaqueteNotaSalidaEconomato
                {
                    IdCreador = request.IdCreador,
                    CantidadPaquete = p.CantidadPaquete,
                    Peso = p.Peso,
                    Tara = p.Tara,
                    Um = p.Um,
                    PesoNeto = p.PesoNeto,
                    PesoBruto = p.PesoBruto
                }).ToList()
            };

            _context.NotaSalidaEconomatos.Add(detalle);

            decimal pendienteDescontar = item.Cantidad;
            foreach (var st in stocksOrigen)
            {
                if (pendienteDescontar <= 0) break;
                decimal aDescontar = Math.Min(st.StockDisponible, pendienteDescontar);
                st.StockDisponible -= aDescontar;
                pendienteDescontar -= aDescontar;
            }
        }

        private async Task CrearDetalleEmpaque(
            int idNotaSalida,
            CreateReq request,
            NotaSalidaFamiliasCreateReq item)
        {
            int sedeOrigenEmp = request.IdSedeOrigen > 0 ? request.IdSedeOrigen : 15;
            var stocksOrigen = await _context.StockEmpaques
                .Where(x => x.IdCompraEmpaque == item.Registro && x.IdSede == sedeOrigenEmp)
                .OrderBy(x => x.IdNotaSalidaEmpaque != null ? 1 : 0)
                .ThenBy(x => x.Id)
                .ToListAsync();

            if (!stocksOrigen.Any())
            {
                stocksOrigen = await _context.StockEmpaques
                    .Where(x => x.IdCompraEmpaque == item.Registro)
                    .OrderBy(x => x.IdNotaSalidaEmpaque != null ? 1 : 0)
                    .ThenBy(x => x.Id)
                    .ToListAsync();
            }

            if (!stocksOrigen.Any())
                throw new BadRequestException($"No existe stock registrado para el empaque {item.Registro}.");

            decimal totalDisponible = stocksOrigen.Sum(s => s.StockDisponible);
            if (totalDisponible < item.Cantidad || totalDisponible <= 0)
                throw new BadRequestException($"Stock insuficiente para el registro {item.Registro}. Stock disponible: {totalDisponible}, Solicitado: {item.Cantidad}.");

            var detalle = new NotaSalidaEmpaque
            {
                IdNotaSalida = idNotaSalida,
                IdCompraEmpaque = item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaEmpaques = item.Paquetes?.Select(p => new PaqueteNotaSalidaEmpaque
                {
                    IdCreador = request.IdCreador,
                    CantidadPaquete = p.CantidadPaquete,
                    Peso = p.Peso,
                    Tara = p.Tara,
                    Um = p.Um,
                    PesoNeto = p.PesoNeto,
                    PesoBruto = p.PesoBruto
                }).ToList()
            };

            _context.NotaSalidaEmpaques.Add(detalle);

            decimal pendienteDescontar = item.Cantidad;
            foreach (var st in stocksOrigen)
            {
                if (pendienteDescontar <= 0) break;
                decimal aDescontar = Math.Min(st.StockDisponible, pendienteDescontar);
                st.StockDisponible -= aDescontar;
                pendienteDescontar -= aDescontar;
            }
        }

        private async Task CrearDetalleProducto(
            int idNotaSalida,
            CreateReq request,
            NotaSalidaFamiliasCreateReq item)
        {
            int sedeOrigenProd = request.IdSedeOrigen > 0 ? request.IdSedeOrigen : 15;
            var stocksOrigen = await _context.StockProductos
                .Where(x => x.IdCompraProducto == item.Registro && x.IdSede == sedeOrigenProd)
                .OrderBy(x => x.IdNotaSalidaProducto != null ? 1 : 0)
                .ThenBy(x => x.Id)
                .ToListAsync();

            if (!stocksOrigen.Any())
            {
                stocksOrigen = await _context.StockProductos
                    .Where(x => x.IdCompraProducto == item.Registro)
                    .OrderBy(x => x.IdNotaSalidaProducto != null ? 1 : 0)
                    .ThenBy(x => x.Id)
                    .ToListAsync();
            }

            if (!stocksOrigen.Any())
                throw new BadRequestException($"No existe stock registrado para el producto {item.Registro}.");

            decimal totalDisponible = stocksOrigen.Sum(s => s.StockDisponible);
            if (totalDisponible < item.Cantidad || totalDisponible <= 0)
                throw new BadRequestException($"Stock insuficiente para el registro {item.Registro}. Stock disponible: {totalDisponible}, Solicitado: {item.Cantidad}.");

            var detalle = new NotaSalidaProducto
            {
                IdNotaSalida = idNotaSalida,
                IdCompraProducto = item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaProductos = item.Paquetes?.Select(p => new PaqueteNotaSalidaProducto
                {
                    IdCreador = request.IdCreador,
                    CantidadPaquete = p.CantidadPaquete,
                    Peso = p.Peso,
                    Tara = p.Tara,
                    Um = p.Um,
                    PesoNeto = p.PesoNeto,
                    PesoBruto = p.PesoBruto
                }).ToList()
            };

            _context.NotaSalidaProductos.Add(detalle);

            decimal pendienteDescontar = item.Cantidad;
            foreach (var st in stocksOrigen)
            {
                if (pendienteDescontar <= 0) break;
                decimal aDescontar = Math.Min(st.StockDisponible, pendienteDescontar);
                st.StockDisponible -= aDescontar;
                pendienteDescontar -= aDescontar;
            }
        }
    }
}
