FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/GATX.sln backend/GATX.sln
COPY backend/src/Gatx.Domain/Gatx.Domain.csproj backend/src/Gatx.Domain/
COPY backend/src/Gatx.Application/Gatx.Application.csproj backend/src/Gatx.Application/
COPY backend/src/Gatx.Infrastructure/Gatx.Infrastructure.csproj backend/src/Gatx.Infrastructure/
COPY backend/src/Gatx.WebApi/Gatx.WebApi.csproj backend/src/Gatx.WebApi/
COPY backend/tests/Gatx.Application.Tests/Gatx.Application.Tests.csproj backend/tests/Gatx.Application.Tests/
RUN dotnet restore backend/GATX.sln
COPY backend backend
RUN dotnet publish backend/src/Gatx.WebApi/Gatx.WebApi.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gatx.WebApi.dll"]
