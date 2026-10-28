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
                try
                {
                    // Módulo Cotización: tablas (antes había que correr SQL/c_cotizaciones.sql a mano)
                    db.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS cotizaciones (
                            id SERIAL PRIMARY KEY,
                            numero VARCHAR(50) NOT NULL DEFAULT '',
                            fecha DATE NOT NULL DEFAULT CURRENT_DATE,
                            sede_id INT NULL,
                            paciente_id INT NULL,
                            ruc_dni VARCHAR(20) NOT NULL DEFAULT '',
                            denominacion VARCHAR(255) NOT NULL DEFAULT '',
                            direccion VARCHAR(255) NULL,
                            telefono VARCHAR(50) NULL,
                            correo VARCHAR(100) NULL,
                            moneda VARCHAR(10) NOT NULL DEFAULT 'S/',
                            descuento_global_porc NUMERIC(10,2) NOT NULL DEFAULT 0,
                            descuento_global NUMERIC(10,2) NOT NULL DEFAULT 0,
                            descuento_item NUMERIC(10,2) NOT NULL DEFAULT 0,
                            descuento_total NUMERIC(10,2) NOT NULL DEFAULT 0,
                            op_exonerada NUMERIC(10,2) NOT NULL DEFAULT 0,
                            op_inafecta NUMERIC(10,2) NOT NULL DEFAULT 0,
                            op_gravada NUMERIC(10,2) NOT NULL DEFAULT 0,
                            igv NUMERIC(10,2) NOT NULL DEFAULT 0,
                            op_gratuita NUMERIC(10,2) NOT NULL DEFAULT 0,
                            otros_cargos NUMERIC(10,2) NOT NULL DEFAULT 0,
                            total NUMERIC(10,2) NOT NULL DEFAULT 0,
                            estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente',
                            enviado_cliente BOOLEAN NOT NULL DEFAULT FALSE,
                            cpe_relacionado VARCHAR(100) NULL,
                            validez_dias INT NOT NULL DEFAULT 15,
                            forma_pago VARCHAR(100) NULL DEFAULT 'Al Contado',
                            observaciones TEXT NULL,
                            creador_id INT NOT NULL DEFAULT 1,
                            modificador_id INT NULL,
                            fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                            fecha_modificacion TIMESTAMP NOT NULL DEFAULT NOW()
                        );

                        CREATE TABLE IF NOT EXISTS cotizaciones_detalle (
                            id SERIAL PRIMARY KEY,
                            cotizacion_id INT NOT NULL REFERENCES cotizaciones(id) ON DELETE CASCADE,
                            producto_id INT NULL,
                            formula_id INT NULL,
                            codigo VARCHAR(50) NULL,
                            descripcion TEXT NOT NULL DEFAULT '',
                            unidad VARCHAR(20) NOT NULL DEFAULT 'NIU',
                            cantidad NUMERIC(10,3) NOT NULL DEFAULT 1,
                            precio_unitario NUMERIC(10,2) NOT NULL DEFAULT 0,
                            descuento NUMERIC(10,2) NOT NULL DEFAULT 0,
                            subtotal NUMERIC(10,2) NOT NULL DEFAULT 0
                        );

                        CREATE INDEX IF NOT EXISTS idx_cotizaciones_sede_fecha ON cotizaciones(sede_id, fecha);
                        CREATE INDEX IF NOT EXISTS idx_cotizaciones_doc ON cotizaciones(ruc_dni, numero);
                        CREATE INDEX IF NOT EXISTS idx_cotizaciones_detalle_cot_id ON cotizaciones_detalle(cotizacion_id);
                    ");
                    Console.WriteLine("--> Base de datos: tablas de cotizaciones verificadas/creadas.");
                }
                catch (Exception exCot)
                {
                    Console.WriteLine($"DB Auto-migration error (cotizaciones): {exCot.Message}");
                }
                try
                {
                    // Lista de precios (Excel PREC. AV): UNA SOLA TABLA (lista_precios_av) con las listas S, A, B y C.
                    // Las tablas anteriores (lista_precios y lista_precios_por_sede) ya no se usan; no se tocan.
                    db.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS lista_precios_av (
                            id SERIAL PRIMARY KEY,
                            codigo_s VARCHAR(30) NULL,
                            codigo_a VARCHAR(30) NULL,
                            codigo_b VARCHAR(30) NULL,
                            codigo_c VARCHAR(30) NULL,
                            producto TEXT NOT NULL DEFAULT '',
                            ifas TEXT NULL,
                            precio_s NUMERIC(10,2) NULL,
                            precio_a NUMERIC(10,2) NULL,
                            precio_b NUMERIC(10,2) NULL,
                            precio_c NUMERIC(10,2) NULL,
                            especialidad1 VARCHAR(100) NULL,
                            especialidad2 VARCHAR(100) NULL,
                            especialidad3 VARCHAR(100) NULL,
                            indicaciones TEXT NULL,
                            presentacion VARCHAR(200) NULL,
                            fecha_ingreso TIMESTAMP NOT NULL DEFAULT NOW(),
                            fecha_modificacion TIMESTAMP NULL,
                            activo BOOLEAN NOT NULL DEFAULT TRUE
                        );
                        CREATE INDEX IF NOT EXISTS idx_lpav_producto ON lista_precios_av(producto);
                        CREATE INDEX IF NOT EXISTS idx_lpav_codigo_a ON lista_precios_av(codigo_a);
                        CREATE INDEX IF NOT EXISTS idx_lpav_especialidad1 ON lista_precios_av(especialidad1);
                    ");

                    if (!db.ListaPrecios.Any())
                    {
                        using var recurso = typeof(DatabaseStartupMigrator).Assembly.GetManifestResourceStream("lista_precios_av.sql");
                        if (recurso != null)
                        {
                            using var lector = new StreamReader(recurso);
                            db.Database.ExecuteSqlRaw(lector.ReadToEnd());
                            Console.WriteLine($"--> Base de datos: lista de precios PREC. AV cargada ({db.ListaPrecios.Count()} productos).");
                        }
                        else
                        {
                            Console.WriteLine("DB Auto-migration: no se encontró el recurso lista_precios_av.sql");
                        }
                    }
                }
                catch (Exception exLp)
                {
                    Console.WriteLine($"DB Auto-migration error (lista_precios_av): {exLp.Message}");
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
