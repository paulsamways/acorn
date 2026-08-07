FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Acorn/Acorn.csproj ./src/Acorn/
COPY src/Acorn.Core/Acorn.Core.csproj ./src/Acorn.Core/
COPY src/Directory.Build.props ./src/
COPY src/Directory.Packages.props ./src/
COPY src/global.json ./src/

RUN dotnet restore ./src/Acorn/Acorn.csproj

COPY src ./src

RUN dotnet publish ./src/Acorn/Acorn.csproj \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Acorn.dll"]
