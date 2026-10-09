using Kova.Application.Interfaces.Repositories;

using Kova.Infrastructure.Persistence;

using Kova.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Oracle.EntityFrameworkCore;
 
namespace Kova.Infrastructure;
 
public static class DependencyInjection

{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)

    {

        services.AddDbContext<KovaDbContext>(options =>

            options.UseOracle(configuration.GetConnectionString("KovaOracle"), oracle =>
                oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));
 
        services.AddScoped<IClienteRepository, ClienteRepository>();

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();

        services.AddScoped<IProdutoRepository, ProdutoRepository>();

        services.AddScoped<IPedidoRepository, PedidoRepository>();

        services.AddScoped<IPagamentoRepository, PagamentoRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
 
        return services;

    }

}
