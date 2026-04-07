using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UUNATRK.Application;
using UUNATRK.Application.Data;
using UUNATRK.Application.Services.Printer;
using UUNATRK.Application.Services.PrintApproval;

namespace UUNATEK.WindowsForm
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            services.AddApplicationServices(configuration);
            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            var printer = serviceProvider.GetRequiredService<PrinterService>();
            var printApprovalService = serviceProvider.GetRequiredService<IPrintApprovalService>();

            Application.Run(new Form1(printer, printApprovalService));
        }
    }
}
