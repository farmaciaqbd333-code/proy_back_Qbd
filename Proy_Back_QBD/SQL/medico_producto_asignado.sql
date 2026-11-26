-- =====================================================================
-- Productos de la Lista de Precios asignados a médicos (relación producto ↔ médico).
-- La asigna el personal QBD desde "Lista de Precios"; la app de médicos la usa para
-- mostrarle a cada médico "sus productos".
-- Distinta de medico_producto_favorito: esos los marca el propio médico en su app.
--
-- medico_id = fila de la tabla medicos elegida (#949...). Como medicos tiene una fila por sede,
-- también se guarda el CMP normalizado (sin espacios ni ceros a la izquierda) para reconocer al
-- mismo médico en cualquier sede.
-- Sin claves foráneas hacia tablas antiguas (igual que app_medico.sql): la relación la controla la API.
-- Idempotente: se ejecuta sola al arrancar la API (Data/DatabaseStartupMigrator.cs).
-- =====================================================================

CREATE TABLE IF NOT EXISTS medico_producto_asignado (
    id SERIAL PRIMARY KEY,
    lista_precio_id INT NOT NULL,
    medico_id INT NOT NULL,
    cmp VARCHAR(20) NULL,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
    creador_id INT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_medico_producto_asignado ON medico_producto_asignado(lista_precio_id, medico_id);
CREATE INDEX IF NOT EXISTS idx_medico_producto_asignado_medico ON medico_producto_asignado(medico_id);
CREATE INDEX IF NOT EXISTS idx_medico_producto_asignado_cmp ON medico_producto_asignado(cmp);
