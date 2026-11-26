# The API with the front end it serves, as one image (see ADR-0006 and docker-compose.yml).

# 1. The front end, built with Node.js into the API's wwwroot folder.
FROM node:22-slim AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

# 2. The API, published with the built front end.
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS api
WORKDIR /src
COPY global.json Directory.Build.props .editorconfig ./
COPY src/FactureQr.Core/FactureQr.Core.csproj src/FactureQr.Core/
COPY src/FactureQr.Persistence/FactureQr.Persistence.csproj src/FactureQr.Persistence/
COPY src/FactureQr.Api/FactureQr.Api.csproj src/FactureQr.Api/
RUN dotnet restore src/FactureQr.Api
COPY src/ src/
COPY --from=web /src/src/FactureQr.Api/wwwroot src/FactureQr.Api/wwwroot
RUN dotnet publish src/FactureQr.Api --no-restore --configuration Release --output /app

# 3. The runtime image: ASP.NET Core only, run as the image's unprivileged user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FactureQr.Api.dll"]
