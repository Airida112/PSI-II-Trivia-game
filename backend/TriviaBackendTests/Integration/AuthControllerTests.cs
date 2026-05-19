using System.Net;
using System.Net.Http.Json;
using TriviaBackend;
using TriviaBackend.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TriviaBackendTests.Integration
{
    [TestFixture]
    public class AuthControllerTests : WebApplicationFactory<Program>
    {
        private TestWebApplicationFactory _factory = null!;
        private HttpClient _client = null!;

        [SetUp]
        public void Setup()
        {
            _factory = new TestWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test]
        public async Task Register_NewUser_ReturnsOk()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "newuser",
                Password = "Password123!"
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var user = await response.Content.ReadFromJsonAsync<BaseUser>();
            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Username, Is.EqualTo("newuser"));
        }

        [Test]
        public async Task Register_ExistingUser_ReturnsConflict()
        {
            await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "existing",
                Password = "Password123!"
            });

            var response = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "existing",
                Password = "Password123!"
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "loginuser",
                Password = "Password123!"
            });

            var response = await _client.PostAsJsonAsync("/api/auth/login", new BaseUserDTO
            {
                Username = "loginuser",
                Password = "Password123!"
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var token = await response.Content.ReadAsStringAsync();
            Assert.That(token, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task Login_WrongPassword_ReturnsBadRequest()
        {
            await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "wrongpass",
                Password = "Password123!"
            });

            var response = await _client.PostAsJsonAsync("/api/auth/login", new BaseUserDTO
            {
                Username = "wrongpass",
                Password = "WrongPassword!"
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task Login_NonExistentUser_ReturnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new BaseUserDTO
            {
                Username = "doesnotexist",
                Password = "Password123!"
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task ElevateToAdmin_ExistingUser_ReturnsOk()
        {
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "player1",
                Password = "Password123!"
            });

            var user = await registerResponse.Content.ReadFromJsonAsync<BaseUser>();
            Assert.That(user, Is.Not.Null);

            // Elevate the player to admin
            var elevateResponse = await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={user!.Id}", new { });

            Assert.That(elevateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var elevatedUser = await elevateResponse.Content.ReadFromJsonAsync<BaseUser>();
            Assert.That(elevatedUser, Is.Not.Null);
            Assert.That(elevatedUser!.Id, Is.EqualTo(user.Id));
            Assert.That(elevatedUser.Username, Is.EqualTo("player1"));
        }

        [Test]
        public async Task ElevateToAdmin_NonExistentUser_ReturnsBadRequest()
        {
            var response = await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id=0", new { });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
