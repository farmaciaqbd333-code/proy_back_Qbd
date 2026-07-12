# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar archivos del proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del código fuente y compilar
COPY . ./
RUN dotnet publish -c Release -o out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV TZ=America/Lima
RUN apt-get update && apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    echo $TZ > /etc/timezone && \
    dpkg-reconfigure -f noninteractive tzdata

# Copiar los archivos publicados desde la etapa de build
COPY --from=build /app/out .

# Exponer el puerto usado por la API
EXPOSE 80

# Comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "Proy_back_QBD.dll"]
