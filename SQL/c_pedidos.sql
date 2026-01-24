SELECT
    Nz (
        F.estadox,
        IIf (Nz (PT.costo_productos, 0) > 0, 'PT', 'NINGUNO')
    ) AS estado,
    P0.Numero AS paciente_id,
    P0.img1,
    P0.img2,
    P0.img3,
    P0.comprobante_electronico,
    P0.fecha_creacion,
    P0.fecha_modificacion,
    creador_id,
    creador_id AS modificador_id,
    Null AS fecha_entrega,
    P0.medico_id,
    C.adelanto AS adelanto,
    (total - adelanto) AS saldo,
    (
        Nz (F.costo_formulas, 0) + Nz (PT.costo_productos, 0)
    ) AS total,
    3 AS sede_id,
    P0.img4,
    Null AS img5,
    Null AS img6,
    P0.recibo,
    P.CuoP AS CuoP
FROM
    (
        (
            (
                SELECT
                    P.CuoP,
                    P.Numero,
                    P.img_receta_1 AS img1,
                    P.img_receta_2 AS img2,
                    P.img_receta_3 AS img3,
                    P.comprobante_electronico AS comprobante_electronico,
                    P.fecha_creacion AS fecha_creacion,
                    P.fecha_creacion AS fecha_modificacion,
                    UCase(P.Usuario) AS creador_id,
                    creador_id AS modificador_id,
                    P.cmp AS medico_id,
                    P.img_receta_4 AS img4,
                    P.boleta AS recibo
                FROM
                    Pedidos AS P
            ) AS P0
            LEFT JOIN (
                SELECT
                    CuoF,
                    SUM(Costo) AS costo_formulas,
                    FIRST (Estado) AS estadox
                FROM
                    Formulas
                GROUP BY
                    CuoF
            ) AS F ON P0.CuoP = F.CuoF
        )
        LEFT JOIN (
            SELECT
                Cuo_P,
                SUM(Importe) AS adelanto
            FROM
                Cobros
            GROUP BY
                Cuo_P
        ) AS C ON P0.CuoP = C.Cuo_P
    )
    LEFT JOIN (
        SELECT
            CuoT,
            SUM(Costo) AS costo_productos
        FROM
            Productos_Terminados
        GROUP BY
            CuoT
    ) AS PT ON P0.CuoP = PT.CuoT;