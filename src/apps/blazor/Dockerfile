FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish ./client/Client.csproj -c Release -o output

FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY --from=build-env /app/output/wwwroot .

COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80