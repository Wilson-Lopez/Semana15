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

| Servicio | Proveedor (plan gratuito) |
|---|---|
| `GatewayApi` | Render, servicio web Docker (`GatewayApi/Dockerfile`) |
| `VacunasApi` | Render, servicio web Docker (`VacunasApi/Dockerfile`) |
| `Veterinaria.Api` | MonsterASP.NET, sitio .NET 10 en IIS |
| Base de datos | MonsterASP.NET, SQL Server (se ejecuta el mismo `VeterinariaDB.sql`) |

La clave JWT y la cadena de conexión no están en el repositorio: se configuran como variables de entorno en cada proveedor (`Jwt__Clave` y `ConnectionStrings__VeterinariaDB`). En los planes gratuitos el primer request puede tardar alrededor de un minuto, porque el servicio se reactiva tras estar inactivo.
