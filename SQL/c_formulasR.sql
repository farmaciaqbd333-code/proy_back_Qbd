SELECT
    codigo AS id,
    descripcion,
    procedimiento,
    aspecto,
    color,
    olor,
    ph,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    Ucase(usuario) AS creador_id,
    creador_id AS modificador_id,
    null AS empaqueId
FROM
    formulasR;