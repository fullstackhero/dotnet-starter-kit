FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o output

FROM nginx:alpine
WORKDIR /var/www/web

COPY --from=build-env /app/output/wwwroot .

COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 7020
EXPOSE 5010

CMD ["nginx", "-g", "daemon off;"]
