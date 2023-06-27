using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Photos;
using Photos.AnalyzerService;
using Photos.AnalyzerService.Abstractions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Photos
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IAnalyzerService, ComputerVisionAnalyzerService>();
        }
    }
}
