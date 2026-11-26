# Veterinaria: microservicios con API Gateway y Ocelot (Semana 15)

Un **API Gateway** con Ocelot es el único punto de entrada a dos microservicios de una clínica veterinaria. El cliente solo conoce la dirección del gateway.

| Proyecto | Qué hace | Puerto local |
|---|---|---|
| `GatewayApi` | API Gateway con Ocelot 25: recibe todas las peticiones, valida el JWT en las rutas protegidas y las reenvía al servicio que corresponde | 5000 |
| `Veterinaria.Api` (microservicio 1) | La API en capas de la Semana 14, sin cambios en su lógica: registro e inicio de sesión (emite el JWT), propietarios y mascotas con permisos por rol, base SQL Server | 5169 |
| `VacunasApi` (microservicio 2) | Catálogo de vacunas para perros y gatos, con datos en memoria. Valida el mismo JWT y exige autenticación en todos sus endpoints | 5170 |

Cada servicio corre en su propio puerto y no comparte base de datos ni clases con el otro. `Veterinaria.Api` sigue formada por `Veterinaria.Entidades`, `Veterinaria.Datos` y `Veterinaria.Negocio`.

```
              Cliente (Postman, .http, navegador)
                            |
                            v
                 GatewayApi :5000  /gateway/...
                 valida el JWT en las rutas con token
                  |                              |
                  v                              v
     Veterinaria.Api :5169  /api/...     VacunasApi :5170  /api/vacunas
     login, propietarios, mascotas       catálogo en memoria
     SQL Server (VeterinariaDB)          valida el JWT
```

## Tabla de rutas (ocelot.json)

| Upstream (cliente → gateway) | Método | Downstream (gateway → servicio) | Token en el gateway |
|---|---|---|---|
| `/gateway/auth/{everything}` | POST | `localhost:5169` `/api/auth/{everything}` | No |
| `/gateway/auth/perfil` | GET | `localhost:5169` `/api/auth/perfil` | No |
| `/gateway/propietarios{everything}` | GET | `localhost:5169` `/api/propietarios{everything}` | No |
| `/gateway/propietarios{everything}` | POST, PUT, DELETE | `localhost:5169` `/api/propietarios{everything}` | Sí |
| `/gateway/mascotas{everything}` | GET | `localhost:5169` `/api/mascotas{everything}` | No |
| `/gateway/mascotas{everything}` | POST, PUT, DELETE | `localhost:5169` `/api/mascotas{everything}` | Sí |
| `/gateway/vacunas{everything}` | GET | `localhost:5170` `/api/vacunas{everything}` | Sí |

- El prefijo `/gateway` se cambia por `/api` y el comodín `{everything}` copia el resto de la ruta (`/1`, `/1/mascotas`, `?especie=Gato`), así no se repite una ruta por cada endpoint.
- "Token en el gateway = Sí" significa que Ocelot valida el JWT (`AuthenticationOptions` con el esquema `Bearer`) antes de reenviar. Si falta o no es válido, responde 401 y la petición no llega al servicio.
- Los GET de `Veterinaria.Api` son públicos en el gateway, pero la API sigue exigiendo el token. El **rol** (Recepcionista o Veterinario) lo sigue exigiendo cada servicio, por eso un token válido con el rol equivocado pasa el gateway y la API responde 403.
- En la nube el gateway carga `ocelot.Production.json`: las mismas rutas, pero con el host público de cada servicio, `https` y el puerto 443.

## Microservicio 2: VacunasApi

| Método | Ruta | Códigos |
|---|---|---|
| GET | `api/vacunas?especie=` | 200, 401 |
| GET | `api/vacunas/{id}` | 200, 401, 404 |

Siete vacunas en memoria (`Datos/VacunasEnMemoria.cs`) con nombre, especie, enfermedades que previene, edad de aplicación, refuerzo y precio. Valida el token con la misma clave, emisor y audiencia que `Veterinaria.Api` y no emite tokens.

## Microservicio 1: Veterinaria.Api (Semana 14)

| Rol | Qué hace en la clínica |
|---|---|
| **Recepcionista** | Registra, actualiza y da de baja a los propietarios, y registra a sus mascotas. |
| **Veterinario** | Actualiza los datos de las mascotas y da de baja sus registros. |

| Endpoint | Sin token | Recepcionista | Veterinario |
|---|---|---|---|
| POST /api/auth/registro, POST /api/auth/login | Sí | Sí | Sí |
| GET /api/auth/perfil, GET de propietarios y mascotas | No (401) | Sí | Sí |
| POST, PUT, DELETE /api/propietarios | No (401) | Sí | No (403) |
| POST /api/mascotas | No (401) | Sí | Sí |
| PUT, DELETE /api/mascotas/{id} | No (401) | No (403) | Sí |

## Cómo ejecutarlo en tu equipo

1. Crear la base local con sus datos de ejemplo (un usuario por rol):

   ```
   sqlcmd -S .\SQLEXPRESS -E -C -f 65001 -i VeterinariaDB.sql
   ```

2. Guardar los secretos con user-secrets (no se suben al repositorio). La clave JWT debe ser **la misma** en los tres proyectos y tener al menos 32 caracteres:

   ```
   dotnet user-secrets set "ConnectionStrings:VeterinariaDB" "<cadena de conexión>" --project Veterinaria.Api
   dotnet user-secrets set "Jwt:Clave" "<clave secreta>" --project Veterinaria.Api
   dotnet user-secrets set "Jwt:Clave" "<clave secreta>" --project VacunasApi
   dotnet user-secrets set "Jwt:Clave" "<clave secreta>" --project GatewayApi
   ```

   El emisor (`Veterinaria.Api`) y la audiencia (`Veterinaria.Clientes`) están en la sección `Jwt` del `appsettings.json` de cada proyecto.

3. Ejecutar los tres servicios, cada uno en su propia terminal:

   ```
   dotnet run --project Veterinaria.Api --launch-profile http
   dotnet run --project VacunasApi
   dotnet run --project GatewayApi
   ```

4. Probar todo por el gateway con `GatewayApi/GatewayApi.http` (REST Client de VS Code): 200, 201, 204, 401, 403, 404 y 502. Para el 502, detén `VacunasApi` y llama a `/gateway/vacunas`. El archivo también llama directo a `VacunasApi` sin token para comprobar que responde 401.

## Despliegue en la nube

| Servicio | Dirección pública | Proveedor (plan gratuito) |
|---|---|---|
| `GatewayApi` | https://gateway-s15.onrender.com/gateway | Render, servicio web Docker (`GatewayApi/Dockerfile`), región Frankfurt |
| `VacunasApi` | https://vacunas-s15.onrender.com/api/vacunas | Render, servicio web Docker (`VacunasApi/Dockerfile`), región Frankfurt |
| `Veterinaria.Api` | http://veterinaria-s15.runasp.net | MonsterASP.NET, sitio .NET 10 en IIS, centro de datos de Alemania |
| Base de datos | (solo accesible para la API y por acceso remoto) | MonsterASP.NET, SQL Server 2025 |

**Por qué estos proveedores:** MonsterASP.NET ofrece gratis un sitio ASP.NET Core y una base **SQL Server**, así que `Veterinaria.Api` y su script se publican sin cambiar el proveedor de EF Core. Su plan gratuito solo permite un sitio, por eso el gateway y `VacunasApi` van en Render, que despliega contenedores Docker desde GitHub con HTTPS incluido. Todos están en Europa (Frankfurt y Alemania) para que el gateway y la API queden cerca.

**Rutas en la nube (`ocelot.Production.json`):**

| Upstream (cliente → gateway) | Método | Downstream (gateway → servicio) | Token en el gateway |
|---|---|---|---|
| `/gateway/auth/{everything}` | POST | `http://veterinaria-s15.runasp.net:80` `/api/auth/{everything}` | No |
| `/gateway/auth/perfil` | GET | `http://veterinaria-s15.runasp.net:80` `/api/auth/perfil` | No |
| `/gateway/propietarios{everything}` | GET | `http://veterinaria-s15.runasp.net:80` `/api/propietarios{everything}` | No |
| `/gateway/propietarios{everything}` | POST, PUT, DELETE | `http://veterinaria-s15.runasp.net:80` `/api/propietarios{everything}` | Sí |
| `/gateway/mascotas{everything}` | GET | `http://veterinaria-s15.runasp.net:80` `/api/mascotas{everything}` | No |
| `/gateway/mascotas{everything}` | POST, PUT, DELETE | `http://veterinaria-s15.runasp.net:80` `/api/mascotas{everything}` | Sí |
| `/gateway/vacunas{everything}` | GET | `https://vacunas-s15.onrender.com:443` `/api/vacunas{everything}` | Sí |

`BaseUrl` = `https://gateway-s15.onrender.com`. El subdominio gratuito `runasp.net` no incluye HTTPS (MonsterASP lo activa a pedido por soporte), por eso el tramo gateway → `Veterinaria.Api` usa `http` y el puerto 80; el tramo hacia `VacunasApi` usa `https` y el 443.

**Secretos:** la clave JWT y la cadena de conexión no están en el repositorio. En MonsterASP se configuran en *Scripting → Environment variables* (`ConnectionStrings__VeterinariaDB`, `Jwt__Clave` y `ASPNETCORE_ENVIRONMENT=Production`) y en Render en *Environment Variables* de cada servicio (`Jwt__Clave`). La clave JWT de la nube es distinta de la local y es la misma en los tres servicios.

**Base de datos en la nube:** se ejecuta por acceso remoto una copia de `VeterinariaDB.sql` sin el bloque `IF DB_ID(...) CREATE DATABASE` ni `USE VeterinariaDB` (las primeras líneas), porque el proveedor ya crea la base con su propio nombre y no permite crear otras. El resto del script (tablas, datos y usuarios) es el mismo. La base se indica con `-d`:

```
sqlcmd -S <servidor>.public.databaseasp.net,1433 -U <usuario> -d <base> -N -C -f 65001 -i VeterinariaDB.nube.sql
```

**El 502 en la nube:** en tu equipo, si `VacunasApi` está detenido nadie escucha en su puerto, Ocelot no puede conectarse y responde 502 Bad Gateway. En Render, en cambio, aunque el servicio esté suspendido responde el proxy de Render (503 "Service Suspended", con la cabecera `x-render-routing`). Para que el cliente reciba lo mismo en los dos casos, el gateway registra un manejador global de Ocelot (`GatewayApi/Manejadores/ServicioNoDisponibleHandler.cs`, con `AddDelegatingHandler`): si la respuesta trae esa cabecera, no vino del microservicio, y el gateway responde 502. Para probarlo, suspende `vacunas-s15` en Render (*Settings → Suspend Web Service*) y llama a `/gateway/vacunas`; luego reactívalo con *Resume Web Service*.

Para probar en la nube, cambia `@Gateway` en `GatewayApi/GatewayApi.http` por `https://gateway-s15.onrender.com/gateway`. En los planes gratuitos el primer request puede tardar alrededor de un minuto, porque el servicio se reactiva tras estar inactivo.
