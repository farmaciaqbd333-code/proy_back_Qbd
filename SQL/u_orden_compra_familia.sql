-- 1. Agregar la nueva columna de ID
ALTER TABLE orden_compra ADD COLUMN id_familia INTEGER;

-- 2. Migrar los datos actuales (Relacionar el texto con el nuevo ID)
UPDATE orden_compra oc
SET id_familia = f.id
FROM familias f
WHERE oc.familia = f.nombre;

-- 3. (Opcional) Si quieres que sea obligatorio
-- ALTER TABLE orden_compra ALTER COLUMN id_familia SET NOT NULL;

-- 4. Agregar la llave foránea
ALTER TABLE orden_compra 
ADD CONSTRAINT fk_orden_compra_familia 
FOREIGN KEY (id_familia) REFERENCES familias(id);
