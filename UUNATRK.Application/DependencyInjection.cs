using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UUNATRK.Application.Data;
using UUNATRK.Application.Models;
using UUNATRK.Application.Repositories;
using UUNATRK.Application.Services.Approval;
using UUNATRK.Application.Services.Printer;
using UUNATRK.Application.Services.PrintApproval;

namespace UUNATRK.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            if (configuration != null)
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
                
                services.Configure<ApprovalServiceSettings>(configuration.GetSection("ApprovalService"));
                services.Configure<PrintRetrySettings>(configuration.GetSection("PrintRetry"));
            }

            if (configuration?.GetSection("Printer").Exists() == true)
                services.Configure<PrinterSettings>(configuration.GetSection("Printer"));
            else
                services.Configure<PrinterSettings>(_ => { });

            services.AddSingleton<PrinterService>();
            services.AddScoped<IPrinterService>(provider => provider.GetRequiredService<PrinterService>());
            services.AddScoped<IRequestLogRepository, RequestLogRepository>();
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IPrintApprovalService, PrintApprovalService>();
            
            var useMockService = configuration?.GetValue<bool>("ApprovalService:UseMockService") ?? true;
            if (useMockService)
            {
                services.AddScoped<IApprovalService, MockApprovalService>();
            }

            return services;
        }
    }
}
