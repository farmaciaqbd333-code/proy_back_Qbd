-- Migra a BCrypt las contraseñas que siguen en texto plano en "usuarios".
-- NO ejecutar en producción sin seguir docs/plan-contrasenas-y-api-key.md.
-- Por seguridad termina en ROLLBACK: cámbialo a COMMIT solo tras revisar los resultados.
-- Uso: psql "<cadena de conexión>" -v ON_ERROR_STOP=1 -f migrar_contrasenas_bcrypt.sql

-- 1. Tipo de la columna. Un hash ocupa 60 caracteres.
SELECT data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'usuarios' AND column_name = 'contrasena';

BEGIN;

-- 2. Solo si el paso 1 muestra una longitud menor que 60 o el tipo "character":
-- ALTER TABLE usuarios ALTER COLUMN contrasena TYPE varchar(255) USING btrim(contrasena);

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 3. Estado inicial.
SELECT
    count(*) AS total,
    count(*) FILTER (WHERE btrim(contrasena) ~ '^\$2[aby]\$[0-9]{2}\$[./A-Za-z0-9]{53}$') AS con_hash,
    count(*) FILTER (WHERE contrasena IS NULL OR btrim(contrasena) = '') AS vacias
FROM usuarios;

-- 4. Conversión idempotente, con el mismo criterio que PasswordHasher: btrim y coste 11.
-- Las contraseñas vacías se dejan como están: esas cuentas no pueden iniciar sesión.
UPDATE usuarios
SET contrasena = crypt(btrim(contrasena), gen_salt('bf', 11))
WHERE contrasena IS NOT NULL
  AND btrim(contrasena) <> ''
  AND btrim(contrasena) !~ '^\$2[aby]\$[0-9]{2}\$[./A-Za-z0-9]{53}$';

-- 5. Debe devolver 0 en pendientes.
SELECT
    count(*) FILTER (
        WHERE contrasena IS NOT NULL
          AND btrim(contrasena) <> ''
          AND btrim(contrasena) !~ '^\$2[aby]\$[0-9]{2}\$[./A-Za-z0-9]{53}$'
    ) AS pendientes,
    count(*) FILTER (WHERE length(btrim(contrasena)) = 60) AS con_hash
FROM usuarios;

ROLLBACK;
