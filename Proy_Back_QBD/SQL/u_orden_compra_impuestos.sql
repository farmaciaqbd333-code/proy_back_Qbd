-- Agregar columnas para Tipo de Operación e Impuesto
ALTER TABLE orden_compra 
ADD COLUMN tipo_operacion VARCHAR(50) DEFAULT 'GRAVADO',
ADD COLUMN incluye_impuesto BOOLEAN DEFAULT FALSE;

-- Actualizar registros existentes si es necesario
UPDATE orden_compra SET tipo_operacion = 'GRAVADO', incluye_impuesto = TRUE WHERE tipo_operacion IS NULL;
