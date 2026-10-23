using Microsoft.EntityFrameworkCore;

namespace Proy_back_QBD.Data
{
    /// <summary>
    /// Ajustes de esquema y correcciones de datos que se ejecutan al arrancar el servidor.
    /// (Antes estaban dentro de Program.cs.)
    /// IMPORTANTE: los UPDATE de datos se vuelven a ejecutar en cada arranque; ver hallazgo M2
    /// de la auditoría antes de agregar más.
    /// </summary>
    public static class DatabaseStartupMigrator
    {
        public static void AplicarCambiosDeEsquemaAlIniciar(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<ApiContext>();
                try
                {
                    // Las contraseñas cifradas (BCrypt, 60 caracteres) requieren columna text
                    db.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS usuarios ALTER COLUMN contrasena TYPE text;");
                }
                catch (Exception exPwd)
                {
                    Console.WriteLine($"DB Auto-migration error (usuarios.contrasena): {exPwd.Message}");
                }
                try
                {
                    // Reparto de cada insumo de fórmula magistral por registro de lote
                    db.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS ""formulasCC"" ADD COLUMN IF NOT EXISTS registros_lote text;");
                    Console.WriteLine("--> Base de datos: columna formulasCC.registros_lote verificada/creada.");
                }
                catch (Exception exReg)
                {
                    Console.WriteLine($"DB Auto-migration error (registros_lote): {exReg.Message}");
                }
                db.Database.ExecuteSqlRaw(@"
                    ALTER TABLE IF EXISTS ""formulasR"" ADD COLUMN IF NOT EXISTS lote text;
                    ALTER TABLE IF EXISTS ""formulasR"" ADD COLUMN IF NOT EXISTS registro text;
                    ALTER TABLE IF EXISTS insumo_producto_intermedio ADD COLUMN IF NOT EXISTS lote text;
                    ALTER TABLE IF EXISTS insumo_producto_intermedio ADD COLUMN IF NOT EXISTS registro text;
                    ALTER TABLE IF EXISTS insumo_producto_intermedio ADD COLUMN IF NOT EXISTS practica numeric;

                    UPDATE nota_salida_insumo
                    SET cantidad = cantidad * 1000,
                        um = 'G',
                        cantidad_recibida = CASE WHEN cantidad_recibida IS NOT NULL AND cantidad_recibida > 0 AND cantidad_recibida < 100 THEN cantidad_recibida * 1000 ELSE cantidad_recibida END
                    WHERE UPPER(um) IN ('KG', 'KILOGRAMOS', 'KGS');

                    UPDATE paquete_nota_salida_insumo
                    SET peso = peso * 1000,
                        tara = tara * 1000,
                        peso_neto = peso_neto * 1000,
                        peso_bruto = peso_bruto * 1000,
                        um = 'G'
                    WHERE UPPER(um) IN ('KG', 'KILOGRAMOS', 'KGS');

                    UPDATE stock_insumo si
                    SET stock_disponible = si.stock_disponible - (ci.cantidad_recibida * 999)
                    FROM compra_insumo ci
                    WHERE ci.id = si.id_compra_insumo
                      AND UPPER(ci.um) IN ('KG', 'KGS', 'KILOGRAMO', 'KILOGRAMOS')
                      AND si.stock_disponible >= 10000
                      AND ci.cantidad_recibida IS NOT NULL
                      AND ci.cantidad_recibida > 0
                      AND si.stock_disponible >= (ci.cantidad_recibida * 999);
                ");
                Console.WriteLine("--> Base de datos: Columnas lote y registro verificadas/creadas correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Auto-migration error: {ex.Message}");
            }
            }
        }
    }
}
