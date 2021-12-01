FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS base
ENV ASPNETCORE_URLS=https://+:5050;http://+:5060
WORKDIR /app
EXPOSE 5050
EXPOSE 5060

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /
COPY ["Directory.Build.props", "/"]
COPY ["src/Host/Host.csproj", "src/Host/"]
COPY ["src/Core/Domain/Domain.csproj", "src/Core/Domain/"]
COPY ["src/Core/Application/Application.csproj", "src/Core/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Migrators/Migrators.MSSQL/Migrators.MSSQL.csproj", "src/Migrators/Migrators.MSSQL/"]
COPY ["src/Migrators/Migrators.MySQL/Migrators.MySQL.csproj", "src/Migrators/Migrators.MySQL/"]
COPY ["src/Migrators/Migrators.PostgreSQL/Migrators.PostgreSQL.csproj", "src/Migrators/Migrators.PostgreSQL/"]
COPY ["src/Shared/Shared.DTOs/Shared.DTOs.csproj", "src/Shared/Shared.DTOs/"]

COPY ["/dotnet.ruleset", "src/Host/"]
COPY ["/dotnet.ruleset", "src/Core/Domain/"]
COPY ["/dotnet.ruleset", "src/Core/Application/"]
COPY ["/dotnet.ruleset", "src/Infrastructure/"]
COPY ["/dotnet.ruleset", "src/Migrators/Migrators.MSSQL/"]
COPY ["/dotnet.ruleset", "src/Migrators/Migrators.MySQL/"]
COPY ["/dotnet.ruleset", "src/Migrators/Migrators.PostgreSQL/"]
COPY ["/dotnet.ruleset", "src/Shared/Shared.DTOs/"]

COPY ["/stylecop.json", "src/Host/"]
COPY ["/stylecop.json", "src/Core/Domain/"]
COPY ["/stylecop.json", "src/Core/Application/"]
COPY ["/stylecop.json", "src/Infrastructure/"]
COPY ["/stylecop.json", "src/Migrators/Migrators.MSSQL/"]
COPY ["/stylecop.json", "src/Migrators/Migrators.MySQL/"]
COPY ["/stylecop.json", "src/Migrators/Migrators.PostgreSQL/"]
COPY ["/stylecop.json", "src/Shared/Shared.DTOs/"]

RUN dotnet restore "src/Host/Host.csproj" --disable-parallel
COPY . .
WORKDIR "/src/Host"
RUN dotnet build "Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
WORKDIR /app/Files
WORKDIR /app
ENTRYPOINT ["dotnet", "DN.WebApi.Host.dll"]
