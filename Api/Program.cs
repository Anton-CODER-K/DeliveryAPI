using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.FakeSmsSender;
using DeliveryAPI.Application.Interfaces;
using DeliveryAPI.Application.Services;
using DeliveryAPI.Application.Verification;
using DeliveryAPI.Infrastructure.Database;
using DeliveryAPI.Infrastructure.Repositories;
using DeliveryAPI.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.Seq;
using System.Text;

namespace DeliveryAPI.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                 .MinimumLevel.Information()
                 .Enrich.FromLogContext()
                 .WriteTo.Console()
                 .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
                 .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();


            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "Delivery API",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "¬веди JWT так: Bearer {токен}"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Services
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<JwtService>();
            builder.Services.AddScoped<DeliveryService>();
            builder.Services.AddScoped<AddressService>();
            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<CategoryService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<PaymentService>();
            builder.Services.AddScoped<LiqPayService>();
            builder.Services.AddScoped<IVerificationCodeGenerator, NumericVerificationCodeGenerator>();
            builder.Services.AddScoped<IVerificationMessageBuilder, SmsVerificationMessageBuilder>();
            builder.Services.AddScoped<INotificationSender, FakeSmsSender>();

            // Repositories
            builder.Services.AddScoped<AuthRepository>();
            builder.Services.AddScoped<DeliveryRepository>();
            builder.Services.AddScoped<AddressRepository>();
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<ProductRepository>();
            builder.Services.AddScoped<CategoryRepository>();
            builder.Services.AddScoped<PaymentRepository>();

            // Database infra
            builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            builder.Services.AddSingleton<IImageStorage>(new LocalImageStorage("/var/www/delivery/images"));


            builder.Services.AddScoped<TransactionExecutor>();


            var jwt = builder.Configuration.GetSection("Jwt");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt["Key"]!)
                        )
                    };
                });

          

            builder.Services.AddAuthorization();


            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            
            app.UseSwagger();
            app.UseSwaggerUI();
            

            


            app.UseHttpsRedirection();

            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardOptions);

            app.UseMiddleware<ExceptionMiddleware>();

            //app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
