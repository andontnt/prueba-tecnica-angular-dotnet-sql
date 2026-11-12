using BonusTest.Application.Bonuses;
using BonusTest.Application.Catalogs;
using Microsoft.Extensions.DependencyInjection;

namespace BonusTest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CatalogService>();
        services.AddScoped<BonusService>();

        return services;
    }
}
