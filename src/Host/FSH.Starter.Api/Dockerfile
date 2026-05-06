# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

COPY src/ ./src/

RUN dotnet restore src/FSH.Starter.slnx
RUN dotnet publish src/Playground/FSH.Starter.Api/FSH.Starter.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "FSH.Starter.Api.dll"]
