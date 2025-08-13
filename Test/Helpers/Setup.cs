using Microsoft.AspNetCore.Hosting;
using Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using projetoapi.Dominio.Interfaces;

namespace Test.Helpers;

public class Setup
{
    public const string PORT = "5001";
    public static TestContext testContext = default!;
    public static WebApplicationFactory<Startup> http = default!;
    public static HttpClient client = default!;

    public static void ClassInit(TestContext testContext)
    {
        Setup.testContext = testContext;
        Setup.http = new WebApplicationFactory<Startup>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("https_port", Setup.PORT)
                       .UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.AddScoped<IAdministradorServico, AdministradorServicoMock>();
                });
            });

        Setup.client = Setup.http.CreateClient();
    }

    public static void ClassCleanup()
    {
        Setup.http.Dispose();
    }
}

public class WebApplicationFactory<T>
{
    public WebApplicationFactory<T> WithWebHostBuilder(Action<IWebHostBuilder> configure)
    {
        throw new NotImplementedException();
    }

    public HttpClient CreateClient()
    {
        throw new NotImplementedException();
    }

    internal void Dispose()
    {
        throw new NotImplementedException();
    }
}