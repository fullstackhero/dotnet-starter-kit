using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using FSH.Framework.Storage.DTOs;
using FSH.Framework.Storage.Local;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Shouldly;

namespace Generic.Tests.Storage;

public class LocalStorageServiceTests
{

    [Fact]
    public async Task UploadAsync_ShouldPrependTenantIdToPath()
    {
        // Arrange
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.WebRootPath.Returns(Path.Combine(Path.GetTempPath(), "wwwroot"));
        Directory.CreateDirectory(environment.WebRootPath);

        var tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        tenantAccessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("test-tenant-123", "Test", null, "admin", null)));

        var service = new LocalStorageService(environment, tenantAccessor);

        var request = new FileUploadRequest
        {
            FileName = "test.png",
            Data = [1, 2, 3]
        };

        // Act
        var resultPath = await service.UploadAsync<object>(request, FSH.Framework.Storage.FileType.Image, CancellationToken.None);

        // Assert
        resultPath.ShouldContain("test-tenant-123");
    }
}
