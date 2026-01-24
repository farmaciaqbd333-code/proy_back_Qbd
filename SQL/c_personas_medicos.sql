SELECT
    null as id,
    cmp as gancho_cmp,
    Datos AS nombreCompleto,
    NULL AS fecha_nacimiento,
    NULL AS dni,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    UCase (Usuario) AS creador_id,
    creador_id AS modificador_id,
    NULL AS telefono,
    NULL AS direccion
FROM
    Medicos;