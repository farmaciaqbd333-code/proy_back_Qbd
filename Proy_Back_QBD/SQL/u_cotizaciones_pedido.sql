-- ==========================================================
-- COTIZACIONES: PEDIDO EN QUE SE CONVIRTIÓ
-- El backend también la crea solo al arrancar (DatabaseStartupMigrator).
-- Ejecutar a mano si se despliega sin reiniciar. Es idempotente.
-- ==========================================================

ALTER TABLE cotizaciones ADD COLUMN IF NOT EXISTS pedido_id INT NULL;

-- Cotizaciones convertidas antes de este cambio: se enlazan con el pedido
-- del mismo paciente y sede creado más cerca de la cotización (máximo 1 día)
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
