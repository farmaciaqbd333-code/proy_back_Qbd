using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Util
{
    public class ZonaHoraria
    {        
        public static DateTime AjustarZona(DateTime fechaInicial)
        {
            TimeZoneInfo ajusteHorario = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            DateTime fechaAjustada = TimeZoneInfo.ConvertTime(fechaInicial, ajusteHorario);
            return fechaAjustada;
        }
        public static DateTime AjustarZonaUTC(DateTime fechaInicial)
        {
            TimeZoneInfo ajusteHorario = TimeZoneInfo.FindSystemTimeZoneById("U");
            DateTime fechaAjustada = TimeZoneInfo.ConvertTime(fechaInicial, ajusteHorario);
            return fechaAjustada;
        }
    }
}