build: ## builds the solution
	dotnet build

start:
	dotnet run --project src/Host/Host.csproj

publish:
	dotnet publish -c Release

publish-to-hub:
	dotnet publish -c Release -p:ContainerRegistry=docker.io -p:ContainerImageName=iammukeshm/dotnet-webapi
