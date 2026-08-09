```bat
@echo off

(
echo namespace Models;
echo.
echo public class PaqueteNotaSalidaEconomato
echo {
echo     public long Id { get; set; }
echo     public long IdNotaSalidaEconomato { get; set; }
echo     public int CantidadPaquete { get; set; }
echo     public decimal? Peso { get; set; }
echo     public decimal? Tara { get; set; }
echo     public string? Um { get; set; }
echo     public decimal? PesoNeto { get; set; }
echo     public decimal? PesoBruto { get; set; }
echo }
) > PaqueteNotaSalidaEconomato.cs

(
echo namespace Models;
echo.
echo public class PaqueteNotaSalidaEmpaque
echo {
echo     public long Id { get; set; }
echo     public long IdNotaSalidaEmpaque { get; set; }
echo     public int CantidadPaquete { get; set; }
echo     public decimal? Peso { get; set; }
echo     public decimal? Tara { get; set; }
echo     public string? Um { get; set; }
echo     public decimal? PesoNeto { get; set; }
echo     public decimal? PesoBruto { get; set; }
echo }
) > PaqueteNotaSalidaEmpaque.cs

(
echo namespace Models;
echo.
echo public class PaqueteNotaSalidaInsumo
echo {
echo     public long Id { get; set; }
echo     public long IdNotaSalidaInsumo { get; set; }
echo     public int CantidadPaquete { get; set; }
echo     public decimal? Peso { get; set; }
echo     public decimal? Tara { get; set; }
echo     public string? Um { get; set; }
echo     public decimal? PesoNeto { get; set; }
echo     public decimal? PesoBruto { get; set; }
echo }
) > PaqueteNotaSalidaInsumo.cs

(
echo namespace Models;
echo.
echo public class PaqueteNotaSalidaProducto
echo {
echo     public long Id { get; set; }
echo     public long IdNotaSalidaProducto { get; set; }
echo     public int CantidadPaquete { get; set; }
echo     public decimal? Peso { get; set; }
echo     public decimal? Tara { get; set; }
echo     public string? Um { get; set; }
echo     public decimal? PesoNeto { get; set; }
echo     public decimal? PesoBruto { get; set; }
echo }
) > PaqueteNotaSalidaProducto.cs

echo.
echo ==========================================
echo   Clases creadas correctamente
echo ==========================================
echo.
pause
```
