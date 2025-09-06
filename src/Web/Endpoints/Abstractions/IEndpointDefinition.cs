using Microsoft.AspNetCore.Builder;

namespace FSH.Framework.Web.Endpoints.Abstractions;

public interface IEndpointDefinition
{
    void DefineEndpoints(WebApplication app);
}