SELECT
    modalidad,
    Cuo_P AS pedido_id,
    importe,
    turno,
    hora AS fecha_creacion,
    hora AS fecha_modificacion,
    UCase (Usuario) AS creador_id,
    UCase (Usuario) AS modificador_id,
    2 AS sede_id,
    CuoC AS id
FROM
    Cobros;