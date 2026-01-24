SELECT
    null as id,
    codigo as gancho,
    Datos AS nombreCompleto,
    NULL AS fecha_nacimiento,
    CMP AS dni,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    '1' AS creador_id,
    '1' AS modificador_id,    
    NULL AS telefono,
    NULL AS direccion
FROM
    Trabajadores AS U
    INNER JOIN Usuarios AS T ON T.Codigo = U.Usuario;
    ;