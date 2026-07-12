SELECT
    Numero AS dni,
    null as id,
    Nombres_Apellidos AS nombreCompleto,
    Fecha_Ncto AS fecha_nacimiento,    
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    Ucase (Usuario) AS creador_id,
    creador_id AS modificador_id,
    telefono,
    direccion
FROM
    Pacientes;