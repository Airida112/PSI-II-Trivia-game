using System.Net;
using Moq;
using NUnit.Framework;
using TriviaBackend;
using TriviaBackend.Hubs;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;
using TriviaBackend.Services.Implementations.DB;
using TriviaBackend.Services.Interfaces;
using TriviaBackend.Services.Interfaces.DB;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            // Remove the real TriviaDbContext
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<TriviaDbContext>) ||
                            d.ServiceType == typeof(ITriviaDbContext))
                .ToList();

            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<TriviaDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            // Add interfaces again for in memory db
            services.AddScoped<ITriviaDbContext, TriviaDbContext>();
            services.AddTransient<IQuestionService, QuestionService>();
            services.AddTransient<IQuestionsService, QuestionsService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPlayerService, PlayerService>();

            services.AddSingleton<ILogger<GameEngineService>>(new Mock<ILogger<GameEngineService>>().Object);

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
