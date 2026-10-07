-- ==========================================================
-- SCRIPT SQL: CREACIÓN DE TABLAS PARA MÓDULO COTIZACIÓN
-- Ejecutar en PostgreSQL (Base de datos Render)
-- ==========================================================

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

-- Índices para búsqueda rápida
CREATE INDEX IF NOT EXISTS idx_cotizaciones_sede_fecha ON cotizaciones(sede_id, fecha);
CREATE INDEX IF NOT EXISTS idx_cotizaciones_doc ON cotizaciones(ruc_dni, numero);
CREATE INDEX IF NOT EXISTS idx_cotizaciones_detalle_cot_id ON cotizaciones_detalle(cotizacion_id);
