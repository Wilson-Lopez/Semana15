# Etapa 1: compila y publica con el SDK de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY VacunasApi.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Etapa 2: imagen final solo con el runtime de ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# No corre como root: usa el usuario sin privilegios que trae la imagen de .NET
USER $APP_UID
# Render indica el puerto en la variable PORT; en otro entorno se usa el 8080
CMD ["sh", "-c", "dotnet VacunasApi.dll --urls http://0.0.0.0:${PORT:-8080}"]
