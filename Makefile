build:
b:
	dotnet build
start:
s:
	dotnet run --project src/Host/Host.csproj
nuget:
n: # del ./nupkg/FullStackHero.WebAPI.Boilerplate.1.0.0.nupkg # dotnet new uninstall FullStackHero.WebAPI.Boilerplate
	nuget pack -NoDefaultExcludes -OutputDirectory nupkg
	dotnet new install ./nupkg/FullStackHero.WebAPI.Boilerplate.1.0.0.nupkg
publish:
	dotnet publish -c Release
publish-to-hub:
	dotnet publish -c Release -p:ContainerRegistry=docker.io -p:ContainerImageName=iammukeshm/dotnet-webapi
tp: # terraform plan
	cd terraform/environments/staging && terraform plan
ta: # terraform apply
	cd terraform/environments/staging && terraform apply
td: # terraform destroy
	cd terraform/environments/staging && terraform destroy
dcu: # docker-compose up : webapi + postgresql
	cd docker-compose/ && docker-compose -f docker-compose.postgresql.yml up -d
dcd: # docker-compose down : webapi + postgresql
	cd docker-compose/ && docker-compose -f docker-compose.postgresql.yml down
fds: # force rededeploy aws ecs service
	aws ecs update-service --force-new-deployment --service dotnet-webapi --cluster fullstackhero