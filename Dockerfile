FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/IdentityHub.Domain/IdentityHub.Domain.csproj src/IdentityHub.Domain/
COPY src/IdentityHub.Application/IdentityHub.Application.csproj src/IdentityHub.Application/
COPY src/IdentityHub.Infrastructure/IdentityHub.Infrastructure.csproj src/IdentityHub.Infrastructure/
COPY src/IdentityHub.API/IdentityHub.API.csproj src/IdentityHub.API/

RUN dotnet restore src/IdentityHub.API/IdentityHub.API.csproj

COPY . .

RUN dotnet publish src/IdentityHub.API/IdentityHub.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "IdentityHub.API.dll"]
