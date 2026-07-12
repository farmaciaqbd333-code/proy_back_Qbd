SELECT
    null as id,
    U.Password AS contrasena,
    NULL AS tipo_id,
    T.Fecha_Creacion AS fecha_creacion,
    T.Fecha_Creacion AS fecha_modificacion,
    '1' AS creador_id,
    '1' AS modificador_id,
    T.horario_entrada,
    T.horario_salida,
    U.Usuario AS persona_id,
    NULL AS cqfp,
    T.horario_almuerzo,
    T.horario_regreso,
    U.Usuario AS codigo,
     AS sedeId
FROM
    Usuarios AS U
    INNER JOIN Trabajadores AS T ON T.Codigo = U.Usuario;