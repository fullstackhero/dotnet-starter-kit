build:
	dotnet build
start:
	dotnet run --project src/Host/Host.csproj
publish:
	dotnet publish -c Release
publish-to-hub:
	dotnet publish -c Release -p:ContainerRegistry=docker.io -p:ContainerImageName=iammukeshm/dotnet-webapi
tp:
	cd terraform/environments/staging && terraform plan
ta:
	cd terraform/environments/staging && terraform apply
td:
	cd terraform/environments/staging && terraform destroy
dcu:
	cd docker-compose/ && docker-compose -f docker-compose.yml up -d