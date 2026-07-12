SELECT
    fecha_emision,
    fecha_vcto,
    elaborado,
    autorizado,
    procedimiento,
    cod_adicional AS cod_term,
    canti_termo,
    aspecto,
    color,
    olor,
    ph,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    Ucase(usuario) AS creador_id,
    creador_id AS modificador_id,
    cod_e AS empaque_id,
    registro AS id,
    3 AS sede_id
FROM
    Laboratorio;    