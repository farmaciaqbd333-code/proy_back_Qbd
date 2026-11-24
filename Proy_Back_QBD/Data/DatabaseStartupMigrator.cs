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
                    // Cálculo de pago por colaborador y mes (solo se crea si no existe)
                    db.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS calculos_pago (
                            id SERIAL PRIMARY KEY,
                            usuario_id INT NOT NULL REFERENCES usuarios(id),
                            anio INT NOT NULL,
                            mes INT NOT NULL,
                            datos TEXT NOT NULL,
                            pago_final NUMERIC(12,2) NULL,
                            modificador_id INT NOT NULL REFERENCES usuarios(id),
                            fecha_modificacion TIMESTAMP NOT NULL,
                            UNIQUE (usuario_id, anio, mes)
                        );");
                }
                catch (Exception exCalculo)
                {
                    Console.WriteLine($"DB Auto-migration error (calculos_pago): {exCalculo.Message}");
                }
                try
                {
                    // Datos de fórmula en las líneas de cotización (se pasan al convertirla en pedido)
                    db.Database.ExecuteSqlRaw(@"
                        ALTER TABLE IF EXISTS cotizaciones_detalle ADD COLUMN IF NOT EXISTS forma_farmaceutica text;
                        ALTER TABLE IF EXISTS cotizaciones_detalle ADD COLUMN IF NOT EXISTS g_por_ml numeric;
                        ALTER TABLE IF EXISTS cotizaciones_detalle ADD COLUMN IF NOT EXISTS unidad_medida text;");
                    Console.WriteLine("--> Base de datos: columnas de fórmula en cotizaciones_detalle verificadas/creadas.");
                }
                catch (Exception exCotDet)
                {
                    Console.WriteLine($"DB Auto-migration error (cotizaciones_detalle): {exCotDet.Message}");
                }
                try
                {
                    // Imagen del comprobante de pago (voucher) de cada orden de compra
                    db.Database.ExecuteSqlRaw(@"ALTER TABLE IF EXISTS compra ADD COLUMN IF NOT EXISTS img_pago text;");
                    Console.WriteLine("--> Base de datos: columna compra.img_pago verificada/creada.");
                }
                catch (Exception exPago)
                {
                    Console.WriteLine($"DB Auto-migration error (compra.img_pago): {exPago.Message}");
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

                        -- Tipo de producto de cada ítem (FM, FMVET, OFI, PT...)
                        ALTER TABLE cotizaciones_detalle ADD COLUMN IF NOT EXISTS tipo_producto VARCHAR(20) NULL;

                        -- Pedido en que se convirtió la cotización
                        ALTER TABLE cotizaciones ADD COLUMN IF NOT EXISTS pedido_id INT NULL;

                    ");
                    Console.WriteLine("--> Base de datos: tablas de cotizaciones verificadas/creadas.");
                }
                catch (Exception exCot)
                {
                    Console.WriteLine($"DB Auto-migration error (cotizaciones): {exCot.Message}");
                }
                try
                {
                    // Cotizaciones convertidas antes de guardar el pedido: se enlazan con el pedido
                    // del mismo paciente y sede creado más cerca de la cotización (máximo 1 día)
                    int enlazadas = db.Database.ExecuteSqlRaw(@"
                        UPDATE cotizaciones c
                        SET pedido_id = (
                            SELECT p.id
                            FROM pedidos p
                            LEFT JOIN pacientes pa ON pa.id = p.paciente_id
                            LEFT JOIN personas pe ON pe.id = pa.persona_id
                            WHERE p.sede_id = c.sede_id
                              AND (p.paciente_id = c.paciente_id OR pe.dni = c.ruc_dni)
                              AND p.fecha_creacion BETWEEN c.fecha_creacion - INTERVAL '1 day' AND c.fecha_creacion + INTERVAL '1 day'
                              AND NOT EXISTS (SELECT 1 FROM cotizaciones o WHERE o.pedido_id = p.id)
                            ORDER BY ABS(EXTRACT(EPOCH FROM (p.fecha_creacion - c.fecha_creacion)))
                            LIMIT 1
                        )
                        WHERE c.estado = 'Convertido' AND c.pedido_id IS NULL;
                    ");
                    if (enlazadas > 0) Console.WriteLine($"--> Cotizaciones convertidas enlazadas con su pedido: {enlazadas}");
                }
                catch (Exception exCotPed)
                {
                    Console.WriteLine($"DB Auto-migration error (cotizaciones.pedido_id): {exCotPed.Message}");
                }

                try
                {
                    // Índices para listados por sede (Gestión de Pedidos, Pacientes)
                    db.Database.ExecuteSqlRaw(@"
                        CREATE INDEX IF NOT EXISTS idx_pedidos_sede_fecha ON pedidos(sede_id, fecha_creacion DESC);
                        CREATE INDEX IF NOT EXISTS idx_pacientes_sede ON pacientes(sede_id);
                    ");
                    Console.WriteLine("--> Base de datos: índices de pedidos y pacientes verificados/creados.");
                }
                catch (Exception exIdx)
                {
                    Console.WriteLine($"DB Auto-migration error (índices): {exIdx.Message}");
                }

                try
                {
                    // Notas de salida de PI: cada línea apunta al producto intermedio exacto
                    // (antes solo se guardaba el lote y el PI se deducía comparando textos).
                    db.Database.ExecuteSqlRaw(@"
                        ALTER TABLE nota_salida_insumo ADD COLUMN IF NOT EXISTS id_producto_intermedio INT NULL;
                        DO $$
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_nota_salida_insumo_producto_intermedio') THEN
                                ALTER TABLE nota_salida_insumo
                                    ADD CONSTRAINT fk_nota_salida_insumo_producto_intermedio
                                    FOREIGN KEY (id_producto_intermedio) REFERENCES producto_intermedio(id) ON DELETE SET NULL;
                            END IF;
                        END $$;
                        CREATE INDEX IF NOT EXISTS idx_nota_salida_insumo_pi ON nota_salida_insumo(id_producto_intermedio);
                    ");

                    // Relleno de líneas antiguas de PI: solo si su lote coincide con UN único PI.
                    // Las ambiguas quedan vacías (ver GET /api/notaSalida/pi-sin-asignar).
                    // Idempotente: solo toca líneas que aún no tienen PI.
                    int rellenadas = db.Database.ExecuteSqlRaw(@"
                        WITH candidatos AS (
                            SELECT nsi.id AS nsi_id, p.id AS pi_id
                            FROM nota_salida_insumo nsi
                            JOIN producto_intermedio p ON UPPER(TRIM(p.lote)) = UPPER(TRIM(nsi.lote))
                            WHERE nsi.id_compra_insumo IS NULL
                              AND nsi.id_producto_intermedio IS NULL
                              AND nsi.lote IS NOT NULL AND TRIM(nsi.lote) <> ''
                        ),
                        unicos AS (
                            SELECT nsi_id, MIN(pi_id) AS pi_id
                            FROM candidatos
                            GROUP BY nsi_id
                            HAVING COUNT(DISTINCT pi_id) = 1
                        )
                        UPDATE nota_salida_insumo t
                        SET id_producto_intermedio = u.pi_id
                        FROM unicos u
                        WHERE t.id = u.nsi_id;
                    ");
                    Console.WriteLine($"--> Notas de salida PI: columna id_producto_intermedio verificada; líneas antiguas completadas ahora: {rellenadas}");
                }
                catch (Exception exNsPi)
                {
                    Console.WriteLine($"DB Auto-migration error (nota_salida_insumo.id_producto_intermedio): {exNsPi.Message}");
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
                        ALTER TABLE lista_precios_av ADD COLUMN IF NOT EXISTS forma_farmaceutica VARCHAR(100) NULL;
                        ALTER TABLE lista_precios_av ADD COLUMN IF NOT EXISTS enfermedades TEXT NULL;
                        ALTER TABLE lista_precios_av ADD COLUMN IF NOT EXISTS modo_uso TEXT NULL;
                        ALTER TABLE lista_precios_av ADD COLUMN IF NOT EXISTS precauciones TEXT NULL;
                        ALTER TABLE lista_precios_av ADD COLUMN IF NOT EXISTS observaciones TEXT NULL;
                        ALTER TABLE lista_precios_av ADD COLUMN IF NOT EXISTS categoria VARCHAR(50) NOT NULL DEFAULT 'FM';
                        CREATE INDEX IF NOT EXISTS idx_lpav_categoria ON lista_precios_av(categoria);
                    ");

                    // QUITADO: la carga automática de datos (SQL/lista_precios_av.sql) cuando la tabla estaba vacía.
                    // El arranque solo crea la estructura; la lista de precios se llena a mano desde el sistema.
                }
                catch (Exception exLp)
                {
                    Console.WriteLine($"DB Auto-migration error (lista_precios_av): {exLp.Message}");
                }
                try
                {
                    // App de médicos: acceso (CMP + contraseña BCrypt), favoritos y pedidos. Solo estructura, idempotente.
                    db.Database.ExecuteSqlRaw(LeerSqlEmbebido("app_medico.sql"));
                    Console.WriteLine("--> Base de datos: tablas de la app de médicos verificadas/creadas.");
                }
                catch (Exception exApp)
                {
                    Console.WriteLine($"DB Auto-migration error (app de médicos): {exApp.Message}");
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

                    -- QUITADO: el UPDATE de stock_insumo que restaba (cantidad_recibida * 999).
                    -- No era idempotente: la condición seguía cumpliéndose después de restar,
                    -- así que volvía a descontar stock en CADA arranque del servidor.
                    -- Si hace falta corregir stocks en KG, hacerlo con un script manual de una sola vez.
                ");
                Console.WriteLine("--> Base de datos: Columnas lote y registro verificadas/creadas correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Auto-migration error: {ex.Message}");
            }
            }
        }

        /// <summary>Lee un script SQL embebido en el ensamblado (ver EmbeddedResource en el .csproj).</summary>
        internal static string LeerSqlEmbebido(string nombre)
        {
            using var stream = typeof(DatabaseStartupMigrator).Assembly.GetManifestResourceStream(nombre)
                ?? throw new InvalidOperationException($"No se encontró el recurso {nombre}.");
            using var lector = new StreamReader(stream);
            return lector.ReadToEnd();
        }
    }
}
