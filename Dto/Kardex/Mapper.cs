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
        public partial void CrearAjusteInsumo(CrearAjusteReq source, AjusteInsumo target);
        public partial void CrearAjusteEmpaque(CrearAjusteReq source, AjusteEmpaque target);

    }
}