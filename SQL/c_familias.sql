CREATE TABLE familias (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    abreviatura VARCHAR(10) NOT NULL,
    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    id_creador INTEGER NOT NULL
);

-- Datos iniciales
INSERT INTO familias (nombre, abreviatura, id_creador) VALUES 
('Insumos', 'INS', 1),
('Materiales', 'MAT', 1),
('Materia Prima', 'MP', 1),
('Envases', 'ENV', 1);
