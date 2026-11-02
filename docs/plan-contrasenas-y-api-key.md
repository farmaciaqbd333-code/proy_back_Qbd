# Plan: migración de contraseñas y rotación de la API key

Nada de este plan se ha ejecutado en producción. Cada fase pide confirmación del responsable antes de empezar.

## Qué cambia con este PR

- `GET /api/usuario` y `GET /api/usuario/{id}` ya no devuelven `contrasena`. Tampoco las respuestas de crear, editar, eliminar y cambiar estado, que serializaban la entidad `Usuario`.
- Las contraseñas nuevas o cambiadas se guardan con BCrypt.
- El login acepta hash y texto plano. Si entra con texto plano, convierte esa contraseña a hash en ese momento (migración perezosa). Si falla el guardado del hash, el login sigue funcionando y queda un error en el log.
- `PUT /api/usuario/{id}` con contraseña vacía o ausente no la cambia.

## Riesgo que el hash no resuelve

Las contraseñas estuvieron accesibles a cualquiera que tuviera la API key, y esa clave está en el bundle público del frontend. No se sabe si alguien las descargó, porque no se revisaron registros de acceso. **Hay que tratarlas como comprometidas**: hashearlas protege la base de datos a partir de ahora, pero no invalida una copia ya obtenida. Por eso la fase 4 pide cambiarlas.

## Parte A: contraseñas

### Fase 1. Comprobaciones previas

No se ha podido ver el esquema real: el repositorio no incluye la definición de la tabla ni migraciones.

1. Ejecutar solo la consulta del paso 1 de `Proy_Back_QBD/SQL/migrar_contrasenas_bcrypt.sql`.
2. Si la columna tiene menos de 60 caracteres o es de tipo `character`, ampliarla **antes de desplegar** con el `ALTER` del paso 2 del script (ejecutado aparte, con su propio `COMMIT`). Sin esto, crear o editar contraseñas devolverá error 500. El login no se bloquea, pero tampoco migra.
3. Hacer una copia cifrada de la base de datos, con acceso restringido. Esa copia contiene las contraseñas en texto plano: borrarla al cerrar la fase 3.

### Fase 2. Desplegar

**El orden importa: primero el backend, luego el frontend.** El frontend nuevo omite `contrasena` si el campo está vacío. El backend actual copiaría ese valor nulo sobre la contraseña, con lo que fallaría el guardado o se borraría la contraseña.

1. Desplegar el backend.
2. Probar: login de una cuenta de prueba, `GET /api/usuario` sin el campo `contrasena` y edición sin cambiar la contraseña.
3. Desplegar el frontend.

El frontend actual sigue funcionando con el backend nuevo, salvo que el formulario de edición pide escribir una contraseña hasta que se despliegue el frontend nuevo.

**Marcha atrás:** el backend anterior no entiende los hashes. Tras volver atrás, los usuarios ya migrados no podrían entrar. Si hay que revertir, restaurar también la columna desde la copia, teniendo en cuenta que se pierden los cambios de contraseña posteriores a ella.

### Fase 3. Migrar el resto

La migración perezosa solo cubre a quien inicia sesión, así que el texto plano de los demás sigue en la tabla hasta este paso.

1. Probar el script en una copia de la base de datos. **El script no se ha ejecutado contra ningún PostgreSQL**: en este equipo no hay servidor. Lo que sí está probado es que el backend verifica hashes con formato `$2a$`, el que genera `pgcrypto`. Comprobar en la copia que una cuenta conocida inicia sesión con la API.
2. Comprobar si el proveedor permite `CREATE EXTENSION pgcrypto`.
3. Revisar los resultados, cambiar el `ROLLBACK` final por `COMMIT` y ejecutarlo en producción. Tarda unos 100-200 ms por usuario y bloquea esas filas mientras dura.
4. Confirmar `pendientes = 0`. Revisar las cuentas con contraseña vacía, que no pueden iniciar sesión.
5. Borrar la copia de la fase 1 cuando ya no haga falta para volver atrás.
6. En un PR posterior, quitar la rama de texto plano de `PasswordHasher.Verificar`.

### Fase 4. Cambiar las contraseñas

Después de la parte B (paso 3), para que las nuevas no se expongan por la misma vía: pedir a todos los usuarios una contraseña nueva, empezando por administradores y cuentas con acceso central. Hoy no existe un flujo de cambio obligatorio; lo más simple es que un administrador las reasigne desde la edición de usuarios.

## Parte B: API key

Rotar la clave **no basta por sí solo**: mientras el navegador la necesite, la nueva también será pública en el bundle. Rotar corta a quien tenga la clave antigua; el arreglo de fondo es el paso 5.

Situación actual: la clave está como valor por defecto en `Middleware/Middleware.cs`, en `appsettings.json` y `appsettings.Development.json` (y por tanto en el historial de git), y repetida en 77 archivos de `src` del frontend.

1. **Frontend:** centralizar la clave en `environment` y un interceptor HTTP, en lugar de un valor por archivo. Sin esto, cada rotación implica tocar decenas de archivos.
2. **Backend:** quitar el valor por defecto y el de `appsettings*.json`, leer `API-KEY` solo de variables de entorno y fallar al arrancar si falta. Aceptar temporalmente también `API-KEY-ANTERIOR`, para rotar sin cortar el servicio.
3. **Rotar:**
   1. Generar una clave aleatoria de al menos 32 bytes (`openssl rand -base64 32`) fuera del repositorio.
   2. En el backend, poner `API-KEY` = nueva y `API-KEY-ANTERIOR` = actual, y desplegar.
   3. Desplegar el frontend con la clave nueva.
   4. Tras un margen para pestañas abiertas, quitar `API-KEY-ANTERIOR`.
4. **Revisar accesos:** consultar los registros del proveedor para `GET /api/usuario` desde orígenes desconocidos, si existen, y decidir si hay que notificar el incidente.
5. **Autenticación real (el arreglo de fondo):** emitir un token de sesión (JWT) en el login y exigirlo en la API con permisos por rol. Hoy cualquiera con la clave puede, sin iniciar sesión, cambiar la contraseña de otro usuario con `PUT /api/usuario/{id}`, crear usuarios o borrarlos. Mientras tanto conviene añadir límite de intentos en `POST /api/usuario/login`, restringir CORS (ahora `AllowAnyOrigin`) y dejar de exponer Swagger en producción.

## Orden recomendado

| # | Paso | Depende de |
|---|------|------------|
| 1 | A1: comprobar la columna y hacer la copia | — |
| 2 | A2: desplegar backend y luego frontend | 1 |
| 3 | A3: migrar el resto | 2 |
| 4 | B1 a B3: centralizar y rotar la API key | 2 |
| 5 | B4: revisar accesos | — (cuanto antes) |
| 6 | A4: cambiar las contraseñas | 4 |
| 7 | B5: autenticación real | 4 |
