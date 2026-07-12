SELECT
    registro AS formula_id,
    cod_i AS insumo_id,
    v AS variable,
    QU AS cantidad_U,
    QL AS cantidad_L,
    pract AS practica,
    csp,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    UCASE(usuario) AS creador_id,
    creador_id AS modificador_id,
    porcentaje,
    2 AS sede_id
FROM
    formulasCC;