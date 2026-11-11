-- ==========================================================
-- COTIZACIONES: ADELANTO DEL CLIENTE
-- Ejecutar en PostgreSQL ANTES de desplegar el backend que usa estas columnas
-- (si el backend sale primero, fallan todas las consultas de cotizaciones).
-- Es idempotente: se puede ejecutar más de una vez.
-- ==========================================================

ALTER TABLE cotizaciones ADD COLUMN IF NOT EXISTS adelanto NUMERIC(10,2) NOT NULL DEFAULT 0;
ALTER TABLE cotizaciones ADD COLUMN IF NOT EXISTS adelanto_medio_pago VARCHAR(50) NULL;
ALTER TABLE cotizaciones ADD COLUMN IF NOT EXISTS adelanto_fecha TIMESTAMP NULL;

-- El adelanto no puede ser negativo ni mayor al total
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_cotizaciones_adelanto') THEN
        ALTER TABLE cotizaciones
            ADD CONSTRAINT ck_cotizaciones_adelanto CHECK (adelanto >= 0 AND adelanto <= total);
    END IF;
END $$;
