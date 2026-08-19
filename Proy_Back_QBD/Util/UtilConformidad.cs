using System;

namespace proy_back_Qbd.Util
{
    public static class UtilConformidad
    {
        public static string CalcularConformidad(DateTime? fechaVencimiento)
        {
            if (fechaVencimiento == null) return "Conforme";
            var hoy = DateTime.Today;
            var fVcto = fechaVencimiento.Value.Date;
            if (fVcto < hoy)
                return "Vencido";
            if (fVcto <= hoy.AddMonths(3))
                return "Por Vencer";
            return "Conforme";
        }

        public static string CalcularConformidad(DateTimeOffset? fechaVencimiento)
        {
            if (fechaVencimiento == null) return "Conforme";
            return CalcularConformidad(fechaVencimiento.Value.DateTime);
        }
    }
}
