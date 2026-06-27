using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Riok.Mapperly.Abstractions;

namespace proy_back_Qbd.Models.Kardex
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public partial class KardexMapper
    {
        public partial AjusteInsumo CrearAjusteInsumo(CrearAjusteReq source);
        public partial AjusteEmpaque CrearAjusteEmpaque(CrearAjusteReq source);

    }
}