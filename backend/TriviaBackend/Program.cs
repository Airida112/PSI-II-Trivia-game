using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TriviaBackend.Data;
using TriviaBackend.Hubs;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Services.Implementations.DB;
using TriviaBackend.Services.Interfaces;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Temporarily add detailed console logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            //log only message with time
            builder.Host.UseSerilog((context, loggerConfig) =>
                loggerConfig
                .MinimumLevel.Error()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}")
                .WriteTo.File(
                    "logs/app.log",
                    outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}",
                    rollingInterval: RollingInterval.Day
                )
            );

            builder.Services.AddControllers();

            // Add detailed errors to SignalR
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:3000", //front http
                        "https://localhost:3001", //front https
                         "https://localhost:5001"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            if (!builder.Environment.IsEnvironment("Test"))
            {
                builder.Configuration.AddEnvironmentVariables();
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddDbContext<TriviaDbContext>(options =>
                    options.UseNpgsql(connectionString));

                builder.Services.AddScoped<ITriviaDbContext>(provider =>
                    provider.GetRequiredService<TriviaDbContext>());
            }

            // Changed to Transient
            builder.Services.AddTransient<IQuestionService, QuestionService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IPlayerService, PlayerService>();
            builder.Services.AddScoped<IQuestionsService, QuestionsService>();
            builder.Services.AddScoped<IClanService, ClanService>();

            builder.Services.AddTransient(typeof(IStatisticsCalculator<,>), typeof(StatisticsCalculator<,>));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (!app.Environment.IsEnvironment("Test"))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
                db.Database.EnsureCreated();
                //Automatically apply ef migrations
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
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<GameHub>("/gamehub");

            app.UseDefaultFiles();  // serve index.html by default
            app.UseStaticFiles();   // serve static files from wwwroot

            // Fallback any non-API routes to index.html for React Router
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}