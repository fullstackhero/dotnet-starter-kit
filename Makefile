.PHONY: help api nswag ui migration migration-update migration-remove kill-ports

help:
	@echo Available targets:
	@echo   make api                    - Run the Web API (https://localhost:7000)
	@echo   make nswag                  - Regenerate NSwag ApiClient.cs (requires API running)
	@echo   make ui                     - Run the Blazor client UI (https://localhost:7100)
	@echo   make migration NAME=msg [DBCONTEXT=dbcontext] - Add EF migration (all or specific DbContext)
	@echo   make migration-update [DBCONTEXT=dbcontext]   - Apply pending EF migrations
	@echo   make migration-remove [DBCONTEXT=dbcontext]   - Remove last EF migration
	@echo   make kill-ports             - Kill processes on all project ports

api:
	dotnet run --project src/api/server/Server.csproj

nswag:
	dotnet build -t:NSwag src/apps/blazor/infrastructure/Infrastructure.csproj

ui:
	dotnet run --project src/apps/blazor/client/Client.csproj

MIGRATE_PROJECT = src/api/migrations/postgresql/PostgreSQL.csproj
MIGRATE_STARTUP = src/api/server/Server.csproj

ifdef DBCONTEXT

migration:
	dotnet ef migrations add "$(NAME)" --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context $(DBCONTEXT) -o $(DBCONTEXT:DbContext=)

migration-update:
	dotnet ef database update --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context $(DBCONTEXT)

migration-remove:
	dotnet ef migrations remove --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context $(DBCONTEXT)

else

migration:
	dotnet ef migrations add "$(NAME)" --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context IdentityDbContext -o Identity
	dotnet ef migrations add "$(NAME)" --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context TenantDbContext -o Tenant
	dotnet ef migrations add "$(NAME)" --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context TodoDbContext -o Todo
	dotnet ef migrations add "$(NAME)" --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context CatalogDbContext -o Catalog

migration-update:
	dotnet ef database update --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context IdentityDbContext
	dotnet ef database update --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context TenantDbContext
	dotnet ef database update --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context TodoDbContext
	dotnet ef database update --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context CatalogDbContext

migration-remove:
	dotnet ef migrations remove --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context IdentityDbContext
	dotnet ef migrations remove --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context TenantDbContext
	dotnet ef migrations remove --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context TodoDbContext
	dotnet ef migrations remove --project $(MIGRATE_PROJECT) --startup-project $(MIGRATE_STARTUP) --context CatalogDbContext

endif

kill-ports:
	pwsh -NoProfile -File scripts/kill-ports.ps1
