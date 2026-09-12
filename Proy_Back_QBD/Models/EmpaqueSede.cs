using Proy_back_QBD.Models;

namespace Proy_back_QBD.Models
{
    public class EmpaqueSede
    {
        public int Id { get; set; }
        public int IdSite { get; set; }
        public int IdEmpaque { get; set; }
        public string? Location { get; set; }
        public decimal? Limite { get; set; }

        public virtual Sede Sede { get; set; } = null!;
        public virtual Empaque Empaque { get; set; } = null!;
    }
}
