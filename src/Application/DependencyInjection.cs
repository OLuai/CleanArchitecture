using System.Reflection;
using CleanArchitecture.Application.Common.Behaviours;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // AutoMapper and MediatR are commercially licensed by Lucky Penny Software. Without a key
        // both still work and only log a warning; set LuckyPennySoftware:LicenseKey (user secrets,
        // environment variable LuckyPennySoftware__LicenseKey, or a vault) once you hold one.
        var licenseKey = builder.Configuration["LuckyPennySoftware:LicenseKey"];

        builder.Services.AddAutoMapper(cfg =>
        {
            cfg.LicenseKey = licenseKey;
            cfg.AddMaps(Assembly.GetExecutingAssembly());
        });

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = licenseKey;
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>));
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });
    }
}
