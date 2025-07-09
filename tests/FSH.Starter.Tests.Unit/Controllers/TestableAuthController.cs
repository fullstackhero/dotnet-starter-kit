using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using FSH.Starter.WebApi.Host;

namespace FSH.Starter.Tests.Unit.Controllers
{
    // Testte property injection için alt sınıf
    public class TestableAuthController : AuthController
    {
        public TestableAuthController(IMediator mediator, HttpContext testHttpContext)
            : base(mediator)
        {
            ControllerContext = new ControllerContext { HttpContext = testHttpContext ?? new DefaultHttpContext() };
        }
    }
}
