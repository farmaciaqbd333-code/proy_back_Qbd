namespace proy_back_Qbd.Util.Familias
{
    public class UtilFamilia
    {
        public static string CodigoInsumo(int id)
        {
            return "MP-QbD-" + id.ToString().PadLeft(4, '0');
        }
        public static string CodigoEmpaque(int id)
        {
            return "ME-QbD-" + id.ToString().PadLeft(4, '0');
        }
    }
}