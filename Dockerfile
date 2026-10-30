FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so restore is cached until a dependency changes
COPY Directory.Build.props ./
COPY src/GeoConquest.Core/GeoConquest.Core.csproj src/GeoConquest.Core/
COPY src/GeoConquest.Infrastructure/GeoConquest.Infrastructure.csproj src/GeoConquest.Infrastructure/
COPY src/GeoConquest.Web/GeoConquest.Web.csproj src/GeoConquest.Web/
RUN dotnet restore src/GeoConquest.Web/GeoConquest.Web.csproj

COPY src/ src/
RUN dotnet publish src/GeoConquest.Web/GeoConquest.Web.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
USER $APP_UID
# Render injects PORT at runtime; Kestrel reads ASPNETCORE_HTTP_PORTS, so map one to the other here
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet GeoConquest.Web.dll"]
