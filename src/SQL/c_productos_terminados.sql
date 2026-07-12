SELECT
    costo,
    cantidad,
    diagnostico,
    aplicacion AS zona_aplicacion,
    estado,
    fecha_creacion,
    fecha_creacion AS fecha_modificacion,
    Ucase(usuario) AS creador_id,
    creador_id AS modificador_id,
    cuot AS pedido_id,
    codigo AS producto_id,
    2 AS sede_id,
    iden AS id
FROM
    Productos_Terminados;