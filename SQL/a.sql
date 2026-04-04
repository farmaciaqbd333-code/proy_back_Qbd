SELECT
    Insumos.Codigo,
    Insumos.Descripcion,
    Insumos.UM,
    IIf(
        (
            SELECT
                SUM(Lote_Estandar * Cantidad)
            FROM
                Elaboracion_Base
            WHERE
                Elaboracion_Base.Codigo = Insumos.Codigo
        ) Is Null,
        0,
(
            SELECT
                SUM(Lote_Estandar * Cantidad)
            FROM
                Elaboracion_Base
            WHERE
                Elaboracion_Base.Codigo = Insumos.Codigo
        )
    ) AS Entradas,
    IIf(
        (
            SELECT
                SUM(Cantidad)
            FROM
                Detalle_NotaSalida
            WHERE
                Detalle_NotaSalida.Codigo_Producto = Insumos.Codigo
        ) Is Null,
        0,
(
            SELECT
                SUM(Cantidad)
            FROM
                Detalle_NotaSalida
            WHERE
                Detalle_NotaSalida.Codigo_Producto = Insumos.Codigo
        )
    ) + IIf(
        (
            SELECT
                SUM(QL)
            FROM
                Detalle_Base
            WHERE
                Detalle_Base.Cod_I = Insumos.Codigo
        ) Is Null,
        0,
(
            SELECT
                SUM(QL)
            FROM
                Detalle_Base
            WHERE
                Detalle_Base.Cod_I = Insumos.Codigo
        )
    ) AS Salidas,
    IIf(
        (
            SELECT
                SUM(Ajuste)
            FROM
                Ajuste
            WHERE
                Ajuste.Codigo = Insumos.Codigo
        ) Is Null,
        0,
(
            SELECT
                SUM(Ajuste)
            FROM
                Ajuste
            WHERE
                Ajuste.Codigo = Insumos.Codigo
        )
    ) AS Ajuste,
    IIf(
        (
            SELECT
                SUM(Baja)
            FROM
                Baja
            WHERE
                Baja.Codigo = Insumos.Codigo
        ) Is Null,
        0,
(
            SELECT
                SUM(Baja)
            FROM
                BAja
            WHERE
                Baja.Codigo = Insumos.Codigo
        )
    ) AS Baja,
    Entradas - Salidas + Ajuste - Baja AS Saldo
FROM
    Insumos
WHERE
    (((Insumos.[Tipo]) = 'PI'))
ORDER BY
    Insumos.Codigo;