## How to Publish Docker Image of the API to Hub

`dotnet publish --os linux --arch x64 -c Release -p:ContainerRegistry=docker.io -p:ContainerRepository=iammukeshm/dotnet-webapi-starter-kit --self-contained`

## How to Generate Dev Certificate for LocalHost / Docker

```
dotnet dev-certs https --clean
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\cert.pfx -p password!
dotnet dev-certs https --trust
```