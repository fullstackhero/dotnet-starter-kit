build: ## builds the solution
	dotnet build

start: ## runs the server
	dotnet run --project src/Host/Host.csproj

publish: ## runs the server
	dotnet publish -c Release