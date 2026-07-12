SELECT
    Id_Especialidad AS id,
    Especialidad AS nombre,
    fecha_creacion AS fecha_modificacion,
    fecha_creacion,
    Ucase(Usuario) AS creador_id,
    creador_id AS modificador_id
FROM
    Especialidad;
    