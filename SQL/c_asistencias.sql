SELECT
    tipo,
    hora_asignada,
    hora_marcada,
    tiempo_atraso,
    tiempo_extra,
    observacion,
    fecha_creacion AS fecha_modificacion,
    fecha_creacion,
    UCase(Usuario) AS creador_id,
    creador_id AS modificador_id,
    2 AS sede_id

FROM Asistencia;
