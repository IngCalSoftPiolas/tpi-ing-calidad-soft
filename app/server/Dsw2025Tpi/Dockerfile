# 1. Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos .csproj de todas las capas para restaurar dependencias de forma eficiente
COPY ["Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj", "Dsw2025Tpi.Api/"]
COPY ["Dsw2025Tpi.Application/Dsw2025Tpi.Application.csproj", "Dsw2025Tpi.Application/"]
COPY ["Dsw2025Tpi.Data/Dsw2025Tpi.Data.csproj", "Dsw2025Tpi.Data/"]
COPY ["Dsw2025Tpi.Domain/Dsw2025Tpi.Domain.csproj", "Dsw2025Tpi.Domain/"]

RUN dotnet restore "Dsw2025Tpi.Api/Dsw2025Tpi.Api.csproj"

# Copiar todo el código fuente de la solución y compilar
COPY . .
WORKDIR "/src/Dsw2025Tpi.Api"
RUN dotnet build "Dsw2025Tpi.Api.csproj" -c Release -o /app/build

# 2. Publicación
FROM build AS publish
RUN dotnet publish "Dsw2025Tpi.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Etapa final / Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Dsw2025Tpi.Api.dll"]