namespace Proy_back_QBD.Security
{
    /// <summary>
    /// Tipos de usuario del sistema (columna usuarios.tipo_id).
    /// Usar estas constantes en lugar de números sueltos.
    /// </summary>
    public static class Roles
    {
        public const int Admin = 1;
        public const int DirectorTecnico = 2;
        public const int AsesorVentas = 3;
        public const int Formulador = 4;
        public const int Supervisor = 8;
        public const int S1 = 9;

        /// <summary>
        /// Para [Authorize(Roles = ...)]: Admin, Supervisor y S1.
        /// (Debe ser una constante de texto; mantener en línea con <see cref="PuedeGestionarUsuarios"/>.)
        /// </summary>
        public const string GestionUsuarios = "1,8,9";

        /// <summary>Solo el administrador.</summary>
        public const string SoloAdmin = "1";

        public static bool PuedeGestionarUsuarios(int rol) => rol is Admin or Supervisor or S1;
    }
}
