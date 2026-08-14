using proy_back_Qbd.Dto.Recepcion;
using proy_back_Qbd.Repositories.Interfaces;
using proy_back_Qbd.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Services
{
    public class RecepcionService : IRecepcionService
    {
        private readonly IRecepcionRepository _repository;

        public RecepcionService(IRecepcionRepository repository)
        {
            _repository = repository;
        }

        public async Task ActualizarRecepcionAsync(int idNotaSalida, RecepcionNotaSalidaReq request)
        {
            var notaSalida = await _repository.GetNotaSalidaByIdAsync(idNotaSalida);
            if (notaSalida == null)
                throw new Exception("Nota de salida no encontrada.");

            notaSalida.Estado = request.Estado ?? notaSalida.Estado;
            notaSalida.FechaRecepcion = request.FechaRecepcion;
            notaSalida.IdUsuarioRecepcion = request.IdUsuarioRecepcion;

            foreach (var familiaReq in request.Familias)
            {
                switch (familiaReq.Familia.ToUpper())
                {
                    case "MP":
                        var insumo = notaSalida.NotaSalidaInsumos?.FirstOrDefault(x => x.Id == familiaReq.IdNotaSalidaFamilia);
                        if (insumo != null)
                        {
                            insumo.CantidadRecibida = familiaReq.CantidadRecibida;
                            foreach (var paqueteReq in familiaReq.Paquetes)
                            {
                                var paquete = insumo.PaqueteNotaSalidaInsumos?.FirstOrDefault(p => p.Id == paqueteReq.IdPaquete);
                                if (paquete != null)
                                {
                                    paquete.CantidadPaqueteRecibida = paqueteReq.CantidadPaqueteRecibida;
                                    paquete.PesoRecibida = paqueteReq.PesoRecibida;
                                    paquete.TaraRecibida = paqueteReq.TaraRecibida;
                                    paquete.PesoNetoRecibida = paqueteReq.PesoNetoRecibida;
                                    paquete.PesoBrutoRecibida = paqueteReq.PesoBrutoRecibida;
                                    paquete.IdVerificador = paqueteReq.IdVerificador;
                                }
                            }
                        }
                        break;
                    case "ME":
                        var empaque = notaSalida.NotaSalidaEmpaques?.FirstOrDefault(x => x.Id == familiaReq.IdNotaSalidaFamilia);
                        if (empaque != null)
                        {
                            empaque.CantidadRecibida = familiaReq.CantidadRecibida;
                            foreach (var paqueteReq in familiaReq.Paquetes)
                            {
                                var paquete = empaque.PaqueteNotaSalidaEmpaques?.FirstOrDefault(p => p.Id == paqueteReq.IdPaquete);
                                if (paquete != null)
                                {
                                    paquete.CantidadPaqueteRecibida = paqueteReq.CantidadPaqueteRecibida;
                                    paquete.PesoRecibida = paqueteReq.PesoRecibida;
                                    paquete.TaraRecibida = paqueteReq.TaraRecibida;
                                    paquete.PesoNetoRecibida = paqueteReq.PesoNetoRecibida;
                                    paquete.PesoBrutoRecibida = paqueteReq.PesoBrutoRecibida;
                                    paquete.IdVerificador = paqueteReq.IdVerificador;
                                }
                            }
                        }
                        break;
                    case "ECO":
                        var economato = notaSalida.NotaSalidaEconomatos?.FirstOrDefault(x => x.Id == familiaReq.IdNotaSalidaFamilia);
                        if (economato != null)
                        {
                            economato.CantidadRecibida = familiaReq.CantidadRecibida;
                            foreach (var paqueteReq in familiaReq.Paquetes)
                            {
                                var paquete = economato.PaqueteNotaSalidaEconomatos?.FirstOrDefault(p => p.Id == paqueteReq.IdPaquete);
                                if (paquete != null)
                                {
                                    paquete.CantidadPaqueteRecibida = paqueteReq.CantidadPaqueteRecibida;
                                    paquete.PesoRecibida = paqueteReq.PesoRecibida;
                                    paquete.TaraRecibida = paqueteReq.TaraRecibida;
                                    paquete.PesoNetoRecibida = paqueteReq.PesoNetoRecibida;
                                    paquete.PesoBrutoRecibida = paqueteReq.PesoBrutoRecibida;
                                    paquete.IdVerificador = paqueteReq.IdVerificador;
                                }
                            }
                        }
                        break;
                    case "PT":
                        var producto = notaSalida.NotaSalidaProductos?.FirstOrDefault(x => x.Id == familiaReq.IdNotaSalidaFamilia);
                        if (producto != null)
                        {
                            producto.CantidadRecibida = familiaReq.CantidadRecibida;
                            foreach (var paqueteReq in familiaReq.Paquetes)
                            {
                                var paquete = producto.PaqueteNotaSalidaProductos?.FirstOrDefault(p => p.Id == paqueteReq.IdPaquete);
                                if (paquete != null)
                                {
                                    paquete.CantidadPaqueteRecibida = paqueteReq.CantidadPaqueteRecibida;
                                    paquete.PesoRecibida = paqueteReq.PesoRecibida;
                                    paquete.TaraRecibida = paqueteReq.TaraRecibida;
                                    paquete.PesoNetoRecibida = paqueteReq.PesoNetoRecibida;
                                    paquete.PesoBrutoRecibida = paqueteReq.PesoBrutoRecibida;
                                    paquete.IdVerificador = paqueteReq.IdVerificador;
                                }
                            }
                        }
                        break;
                }
            }

            await _repository.GuardarCambiosAsync();
        }
    }
}
