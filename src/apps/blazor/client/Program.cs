using FSH.Starter.Blazor.Client;
using FSH.Starter.Blazor.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FSH.OfflineSync.Extensions;
using FSH.Starter.Blazor.Infrastructure.Storage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddClientServices(builder.Configuration);

/*
 FSH.OfflineSync

A new amazing package 

 Sample Scenario: Offline Order Submission

User fills a form in offline mode

      Clicks Submit "Post/Put/Delete" ? request is stored locally

                 App comes online again

      On "Get" request ?response is stored locally

                 App can use it anytime while it offline

All stored requests are sent to the backend transparently

        https://www.nuget.org/packages/FSH.OfflineSync/1.0.0

 */


builder.Services.AddOfflineSyncHttpClient(builder.Configuration, options =>
{
    options.AuthTokenKey = StorageConstants.Local.AuthToken; // or "authToken" Or StorageConstants.Local.AuthToken for FullStackHero
});

await builder.Build().RunAsync();
