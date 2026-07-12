SELECT 
formulas.costo AS costo,
 cantidad, formula_magistral,
  FF AS formula_farmaceutica,
   [g/ml], UM AS unidad_medida,
    lote,
     diagnostico,
      zona_aplicacion,
       estado,
        reportado, 
        Fecha_Creacion, 
        Fecha_Creacion AS fecha_modificacion, 
        Ucase(usuario) AS creador_id,
         creador_id AS modificador_id,
          Registro AS id,
           null AS inserto,
            2 AS sede_id,
             CuoF AS pedido_id
FROM formulas;