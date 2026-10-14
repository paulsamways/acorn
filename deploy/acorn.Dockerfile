FROM node:24-alpine AS build-js
WORKDIR /src/Acorn.Client

COPY src/Acorn.Client/package.json src/Acorn.Client/package-lock.json ./
RUN npm ci

COPY src/Acorn.Client ./

# Output path in the webpack config resolves to ../Acorn/wwwroot/js/pages/
RUN npx webpack --config webpack.production.config.js

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

COPY src/Acorn/Acorn.csproj ./src/Acorn/
COPY src/Acorn.Core/Acorn.Core.csproj ./src/Acorn.Core/
COPY src/Directory.Build.props ./src/
COPY src/Directory.Packages.props ./src/
COPY ./global.json ./

RUN dotnet restore ./src/Acorn/Acorn.csproj

COPY src ./src
COPY --from=build-js /src/Acorn/wwwroot/js/pages ./src/Acorn/wwwroot/js/pages

RUN --mount=type=secret,id=sixlabors,env=SIXLABORS_LICENSE_KEY \
  dotnet publish ./src/Acorn/Acorn.csproj \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Acorn.dll"]
