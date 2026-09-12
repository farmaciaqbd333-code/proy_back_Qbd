using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Models
{
    public class EconomatoSede
    {
        public int Id { get; set; }
        public int IdSite { get; set; }
        public int IdEconomato { get; set; }
        public string? Location { get; set; }
        public decimal? Limite { get; set; }

        public virtual Sede Sede { get; set; } = null!;
        public virtual Economato Economato { get; set; } = null!;
    }
}
