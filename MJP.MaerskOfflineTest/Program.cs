namespace MJP.MaerskOfflineTest
{
    using Microsoft.Extensions.PlatformAbstractions;
    using MJP.MaerskOfflineTest.Services;
    using MJP.MaerskOfflineTest.Services.Interfaces;
    using System.Reflection;
    using System.Text.Json.Serialization;

    public class Program
    {
        public static void Main(string[] args)
        {
            var app = CreateConfiguredBuilder(args).Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapControllers();
            app.Run();
        }

        public static WebApplicationBuilder CreateConfiguredBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IExchangeRateService, ExchangeRateService>();

            builder.Services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Program).GetTypeInfo().Assembly.GetName().Name + ".xml";
                // It's just easier to use the same XML comments as VisualStudio does. This allows Swagger to do exactly that.
                options.IncludeXmlComments(Path.Combine(basePath, fileName));
            });

            return builder;
        }
    }
}