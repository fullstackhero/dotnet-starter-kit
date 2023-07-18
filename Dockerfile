# Use the official .NET 7 SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# Set the working directory to /app
WORKDIR /app

# Copy the .csproj files and restore dependencies
COPY *.sln .
COPY src/Core/Domain/*.csproj .Domain/
COPY src/Core/Application/*.csproj .Application/
COPY src/Infrastructure/*.csproj .Infrastructure/
COPY src/Host/*.csproj .Host/

# Copy the remaining source code and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official runtime image for .NET 7
FROM mcr.microsoft.com/dotnet/aspnet:7.0

# Set the working directory to /app
WORKDIR /app

# Copy the built application from the build environment
COPY --from=build-env /app/out .

# Set the entry point to run the Web API DLL file
ENTRYPOINT ["dotnet", "FSH.WebApi.Host.dll"]