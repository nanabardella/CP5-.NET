using System.Reflection;
using System.Threading.RateLimiting;
using System.Globalization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Kova.api.Exceptions;
using Kova.api.HealthChecks;
using Kova.Application.Interfaces.Services;
using Kova.Application.Services;
using Kova.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(2, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(new QueryStringApiVersionReader("api-version"), new HeaderApiVersionReader("X-Api-Version"));
}).AddMvc().AddApiExplorer(options => options.GroupNameFormat = "'v'VVVV");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("produto-write", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, token) =>
    {
        var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)) : 60;
        context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 429, Title = "Limite de requisições excedido",
            Detail = "São permitidas 10 requisições por IP a cada minuto. Tente novamente após Retry-After."
        }, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: token);
    };
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddKovaHealthChecks();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    foreach (var version in new[] { "v1.0", "v2.0" })
        options.SwaggerDoc(version, new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "KOVA E-commerce API", Version = version,
            Description = version == "v1.0" ? "Versão 1.0 DEPRECADA: listagem antiga de produtos." : "Versão 2.0: produtos paginados."
        });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), includeControllerXmlComments: true);
});
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in app.Services.GetRequiredService<IApiVersionDescriptionProvider>().ApiVersionDescriptions)
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName + (description.IsDeprecated ? " (deprecada)" : ""));
    });
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthorization();
app.UseKovaHealthChecks();
app.MapControllers();
app.Run();
