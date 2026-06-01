using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Riok.Mapperly.Abstractions;

namespace proy_back_Qbd.Models
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class PaqueteMapper
    {
        public static partial Paquete CrearPaqueteInsumo(PaqueteInsumoCrearReq source);
        public static partial Paquete CrearPaqueteEmpaque(PaqueteEmpaqueCrearReq source);
        public static partial void ModificarPaqueteInsumo(PaqueteInsumoModificarReq source, Paquete paquete);
        public static partial void ModificarPaqueteEmpaque(PaqueteEmpaqueModificarReq source, Paquete paquete);
    }
}