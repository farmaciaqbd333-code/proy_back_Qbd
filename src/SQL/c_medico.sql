SELECT
    Especialidad AS especialidad_id,
    numero_especialidad,
    null AS persona_id,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    Ucase(Usuario) AS creador_id,
    creador_id AS modificador_id,
    cmp
FROM
    Medicos;