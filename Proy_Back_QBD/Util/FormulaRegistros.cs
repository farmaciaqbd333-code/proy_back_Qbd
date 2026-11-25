using System.Collections.Generic;
using System.Linq;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Util
{
    /// <summary>
    /// Una línea de fórmula solo descuenta stock si tiene registro (lote) en RegistrosLote.
    /// Al MODIFICAR una fórmula se borran y se vuelven a crear sus líneas; si el front no manda el registro
    /// (p. ej. no llegó a cargar los lotes), se conserva el que ya tenía esa línea para que no deje de descontar.
    /// </summary>
    public static class FormulaRegistros
    {
        public static void ConservarRegistros(FormulaCC nueva, IEnumerable<FormulaCC> anteriores)
        {
            if (!string.IsNullOrWhiteSpace(nueva.RegistrosLote)) return;
            var previa = anteriores.FirstOrDefault(a => a.InsumoId == nueva.InsumoId && a.Variable == nueva.Variable && !string.IsNullOrWhiteSpace(a.RegistrosLote))
                      ?? anteriores.FirstOrDefault(a => a.InsumoId == nueva.InsumoId && !string.IsNullOrWhiteSpace(a.RegistrosLote));
            if (previa != null) nueva.RegistrosLote = previa.RegistrosLote;
        }
    }
}
