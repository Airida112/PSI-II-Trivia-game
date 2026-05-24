using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using TriviaBackend.Data;
using TriviaBackend.Hubs;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Services.Implementations.DB;
using TriviaBackend.Services.Interfaces;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend
{
    /// <summary>
    /// Maps the JWT NameIdentifier claim to the SignalR user id,
    /// enabling Clients.User(userId) in the hub and controllers.
    /// </summary>
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
            => connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.StaticFiles", LogLevel.Error); 
            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);


            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.File(
                    "logs/app.log",
                    outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}",
                    rollingInterval: RollingInterval.Day
                )
                .CreateLogger();

            builder.Logging.AddSerilog();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            var jwtKey = builder.Configuration.GetValue<string>("AppSettings:Token")
                ?? throw new InvalidOperationException("AppSettings:Token is not configured.");
            var jwtIssuer = builder.Configuration.GetValue<string>("AppSettings:Issuer");
            var jwtAudience = builder.Configuration.GetValue<string>("AppSettings:Audience");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gamehub"))
                                context.Token = accessToken;

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:3000",
                        "https://localhost:3001",
                        "https://localhost:5001"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            builder.Configuration.AddEnvironmentVariables();
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=triviaDb;Username=postgres;Password=postgres";

            builder.Services.AddDbContext<TriviaDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<ITriviaDbContext>(provider =>
                provider.GetRequiredService<TriviaDbContext>());

            builder.Services.AddTransient<IQuestionService, QuestionService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IQuestionsService, QuestionsService>();
            builder.Services.AddScoped<IClanService, ClanService>();
            builder.Services.AddTransient(typeof(IStatisticsCalculator<,>), typeof(StatisticsCalculator<,>));

            builder.Services.AddSingleton<IPresenceService, PresenceService>();
            builder.Services.AddScoped<IFriendshipService, FriendshipService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (!app.Environment.IsEnvironment("Test"))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                db.Database.Migrate();
            }

            var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();
            GameHub.SetHubContext(hubContext);
            GameHub.SetServiceProvider(app.Services);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<GameHub>("/gamehub");

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
