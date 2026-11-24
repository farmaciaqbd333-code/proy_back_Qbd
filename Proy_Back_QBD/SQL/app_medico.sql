-- =====================================================================
-- App de médicos (React Native / Expo)
-- Tablas: acceso del médico (CMP + contraseña BCrypt), favoritos y pedidos.
-- Idempotente: se ejecuta sola al arrancar la API (Data/DatabaseStartupMigrator.cs).
-- Se puede correr a mano en PostgreSQL si se prefiere crear antes del despliegue.
-- Sin claves foráneas hacia tablas antiguas (medicos, sedes, pedidos, usuarios, lista_precios_av):
-- en producción algunas no tienen clave primaria y el FK hacía fallar la creación de TODAS las tablas.
-- La relación la controla la API. Cada sentencia se ejecuta por separado al arrancar.
-- =====================================================================

-- Acceso a la app: UNO por CMP (la tabla medicos tiene una fila por sede).
-- cmp va normalizado: sin espacios y sin ceros a la izquierda.
CREATE TABLE IF NOT EXISTS medico_acceso (
    id SERIAL PRIMARY KEY,
    cmp VARCHAR(20) NOT NULL,
    contrasena_hash TEXT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    debe_cambiar BOOLEAN NOT NULL DEFAULT TRUE,
    version_token INT NOT NULL DEFAULT 1,
    intentos_fallidos INT NOT NULL DEFAULT 0,
    bloqueado_hasta TIMESTAMP NULL,
    ultimo_ingreso TIMESTAMP NULL,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMP NOT NULL DEFAULT NOW(),
    modificador_id INT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_medico_acceso_cmp ON medico_acceso(cmp);

-- Favoritos del médico (relación médico ↔ producto de lista_precios_av). No restringe el catálogo.
CREATE TABLE IF NOT EXISTS medico_producto_favorito (
    id SERIAL PRIMARY KEY,
    cmp VARCHAR(20) NOT NULL,
    lista_precio_id INT NOT NULL,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_medico_favorito ON medico_producto_favorito(cmp, lista_precio_id);

-- Pedidos hechos desde la app (solicitudes que el personal atiende en "Pedidos de médicos")
CREATE TABLE IF NOT EXISTS pedidos_medico (
    id SERIAL PRIMARY KEY,
    medico_id INT NOT NULL,
    cmp VARCHAR(20) NOT NULL,
    sede_id INT NULL,
    paciente_nombre VARCHAR(200) NULL,
    paciente_dni VARCHAR(12) NULL,
    observacion TEXT NULL,
    estado VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE',
    respuesta TEXT NULL,
    total NUMERIC(12,2) NOT NULL DEFAULT 0,
    pedido_id INT NULL,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMP NOT NULL DEFAULT NOW(),
    modificador_id INT NULL
);
CREATE INDEX IF NOT EXISTS idx_pedidos_medico_cmp ON pedidos_medico(cmp, fecha_creacion DESC);
CREATE INDEX IF NOT EXISTS idx_pedidos_medico_sede_estado ON pedidos_medico(sede_id, estado);

CREATE TABLE IF NOT EXISTS pedidos_medico_detalle (
    id SERIAL PRIMARY KEY,
    pedido_medico_id INT NOT NULL REFERENCES pedidos_medico(id) ON DELETE CASCADE,
    lista_precio_id INT NULL,
    codigo VARCHAR(30) NULL,
    producto TEXT NOT NULL,
    cantidad INT NOT NULL CHECK (cantidad > 0),
    precio_unitario NUMERIC(10,2) NOT NULL DEFAULT 0,
    subtotal NUMERIC(12,2) NOT NULL DEFAULT 0,
    observacion TEXT NULL
);
CREATE INDEX IF NOT EXISTS idx_pedidos_medico_detalle_pedido ON pedidos_medico_detalle(pedido_medico_id);
