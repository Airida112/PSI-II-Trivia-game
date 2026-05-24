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
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TriviaDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<TriviaDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}
