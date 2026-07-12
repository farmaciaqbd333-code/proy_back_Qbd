SELECT
    codigoFR AS formulaR_id,
    codigo AS insumo_id,
    porcentaje,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    Ucase(usuario) AS creador_id,
    creador_id AS modificador_id
FROM
    InsumosR;
    