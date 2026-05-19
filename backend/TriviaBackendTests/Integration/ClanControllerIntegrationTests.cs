using System.Net;
using System.Net.Http.Json;
using TriviaBackend;
using TriviaBackend.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TriviaBackendTests.Integration
{
    [TestFixture]
    public class ClanControllerIntegrationTests : WebApplicationFactory<Program>
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
        public async Task GetUserByName_ExistingUser_ReturnsOk()
        {
            // Arrange
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "testuser123",
                Password = "Password123!"
            });
            Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Act
            var response = await _client.GetAsync("/api/clan/getuser/testuser123");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var user = await response.Content.ReadFromJsonAsync<BaseUser>();
            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Username, Is.EqualTo("testuser123"));
        }

        [Test]
        public async Task GetUserByName_NonExistentUser_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/clan/getuser/nonexistent");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task CreateClan_AsAdmin_ReturnsOk()
        {
            // Arrange - Create and elevate user to admin
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "adminuser",
                Password = "Password123!"
            });
            var user = await registerResponse.Content.ReadFromJsonAsync<BaseUser>();

            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={user!.Id}", new { });

            // Act
            var response = await _client.PostAsync($"/api/clan/create?clanName=TestClan&userId={user.Id}", null);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task CreateClan_AsNonAdmin_ReturnsUnauthorized()
        {
            // Arrange - Create regular player
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "regularuser",
                Password = "Password123!"
            });
            var user = await registerResponse.Content.ReadFromJsonAsync<BaseUser>();

            // Act
            var response = await _client.PostAsync($"/api/clan/create?clanName=TestClan2&userId={user!.Id}", null);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task GetClanById_ExistingClan_ReturnsOk()
        {
            // Arrange - Create admin and clan
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "clanAdmin",
                Password = "Password123!"
            });
            var user = await registerResponse.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={user!.Id}", new { });

            await _client.PostAsync($"/api/clan/create?clanName=GetByIdClan&userId={user.Id}", null);

            // Get clan by name first to get ID
            var clanByNameResponse = await _client.GetAsync("/api/clan/getclanbyname/GetByIdClan");
            var clan = await clanByNameResponse.Content.ReadFromJsonAsync<Clan>();

            // Act
            var response = await _client.GetAsync($"/api/clan/getclan/{clan!.Id}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var retrievedClan = await response.Content.ReadFromJsonAsync<Clan>();
            Assert.That(retrievedClan!.Name, Is.EqualTo("GetByIdClan"));
        }

        [Test]
        public async Task GetClanById_NonExistentClan_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/clan/getclan/99999");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task GetClanByName_ExistingClan_ReturnsOk()
        {
            // Arrange
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "nameSearchAdmin",
                Password = "Password123!"
            });
            var user = await registerResponse.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={user!.Id}", new { });

            await _client.PostAsync($"/api/clan/create?clanName=NameSearchClan&userId={user.Id}", null);

            // Act
            var response = await _client.GetAsync("/api/clan/getclanbyname/NameSearchClan");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var clan = await response.Content.ReadFromJsonAsync<Clan>();
            Assert.That(clan!.Name, Is.EqualTo("NameSearchClan"));
        }

        [Test]
        public async Task GetClanByName_NonExistentClan_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/clan/getclanbyname/NonExistentClan");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task JoinClan_ValidRequest_ReturnsOk()
        {
            // Arrange - Create clan
            var adminRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "joinClanAdmin",
                Password = "Password123!"
            });
            var admin = await adminRegister.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={admin!.Id}", new { });
            await _client.PostAsync($"/api/clan/create?clanName=JoinTestClan&userId={admin.Id}", null);

            // Create player
            var playerRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "joiningPlayer",
                Password = "Password123!"
            });
            var player = await playerRegister.Content.ReadFromJsonAsync<BaseUser>();

            // Get clan ID
            var clanResponse = await _client.GetAsync("/api/clan/getclanbyname/JoinTestClan");
            var clan = await clanResponse.Content.ReadFromJsonAsync<Clan>();

            // Act
            var response = await _client.PostAsync($"/api/clan/join/{clan!.Id}?userId={player!.Id}", null);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task JoinClan_NonExistentUser_ReturnsNotFound()
        {
            var response = await _client.PostAsync("/api/clan/join/1?userId=nonexistent", null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task JoinClan_NonExistentClan_ReturnsNotFound()
        {
            // Create user
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "noClanUser",
                Password = "Password123!"
            });
            var user = await registerResponse.Content.ReadFromJsonAsync<BaseUser>();

            var response = await _client.PostAsync($"/api/clan/join/99999?userId={user!.Id}", null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task LeaveClan_ValidRequest_ReturnsOk()
        {
            // Arrange - Create clan and join
            var adminRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "leaveClanAdmin",
                Password = "Password123!"
            });
            var admin = await adminRegister.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={admin!.Id}", new { });
            await _client.PostAsync($"/api/clan/create?clanName=LeaveTestClan&userId={admin.Id}", null);

            var playerRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "leavingPlayer",
                Password = "Password123!"
            });
            var player = await playerRegister.Content.ReadFromJsonAsync<BaseUser>();

            var clanResponse = await _client.GetAsync("/api/clan/getclanbyname/LeaveTestClan");
            var clan = await clanResponse.Content.ReadFromJsonAsync<Clan>();

            await _client.PostAsync($"/api/clan/join/{clan!.Id}?userId={player!.Id}", null);

            // Act
            var response = await _client.DeleteAsync($"/api/clan/leave/{clan.Id}?userId={player.Id}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task LeaveClan_UserNotInClan_ReturnsBadRequest()
        {
            // Arrange
            var adminRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "leaveNotInAdmin",
                Password = "Password123!"
            });
            var admin = await adminRegister.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={admin!.Id}", new { });
            await _client.PostAsync($"/api/clan/create?clanName=LeaveNotInClan&userId={admin.Id}", null);

            var playerRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "notInClanPlayer",
                Password = "Password123!"
            });
            var player = await playerRegister.Content.ReadFromJsonAsync<BaseUser>();

            var clanResponse = await _client.GetAsync("/api/clan/getclanbyname/LeaveNotInClan");
            var clan = await clanResponse.Content.ReadFromJsonAsync<Clan>();

            // Act
            var response = await _client.DeleteAsync($"/api/clan/leave/{clan!.Id}?userId={player!.Id}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task RenameClan_AsAdmin_ReturnsOk()
        {
            // Arrange
            var adminRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "renameAdmin",
                Password = "Password123!"
            });
            var admin = await adminRegister.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={admin!.Id}", new { });
            await _client.PostAsync($"/api/clan/create?clanName=OldClanName&userId={admin.Id}", null);

            var clanResponse = await _client.GetAsync("/api/clan/getclanbyname/OldClanName");
            var clan = await clanResponse.Content.ReadFromJsonAsync<Clan>();

            // Act
            var response = await _client.PatchAsync($"/api/clan/rename?clanId={clan!.Id}&newName=NewClanName&userId={admin.Id}", null);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task DeleteClan_AsAdmin_ReturnsNoContent()
        {
            // Arrange
            var adminRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "deleteAdmin",
                Password = "Password123!"
            });
            var admin = await adminRegister.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={admin!.Id}", new { });
            await _client.PostAsync($"/api/clan/create?clanName=ClanToDelete&userId={admin.Id}", null);

            var clanResponse = await _client.GetAsync("/api/clan/getclanbyname/ClanToDelete");
            var clan = await clanResponse.Content.ReadFromJsonAsync<Clan>();

            // Act
            var response = await _client.DeleteAsync($"/api/clan/delete?clanId={clan!.Id}&userId={admin.Id}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
        [Test]
        public async Task KickUser_AsAdmin_ReturnsOk()
        {
            // Arrange - create admin, elevate and clan
            var adminRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "kickAdmin",
                Password = "Password123!"
            });
            var admin = await adminRegister.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={admin!.Id}", new { });
            await _client.PostAsync($"/api/clan/create?clanName=KickClan&userId={admin.Id}", null);

            // create player and join
            var playerRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "playerToKick",
                Password = "Password123!"
            });
            var player = await playerRegister.Content.ReadFromJsonAsync<BaseUser>();

            var clanResponse = await _client.GetAsync("/api/clan/getclanbyname/KickClan");
            var clan = await clanResponse.Content.ReadFromJsonAsync<Clan>();

            await _client.PostAsync($"/api/clan/join/{clan!.Id}?userId={player!.Id}", null);

            // Act
            var response = await _client.PostAsync($"/api/clan/kick?kickeeId={player.Id}&adminId={admin.Id}", null);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task KickUser_AsNonAdmin_ReturnsUnauthorized()
        {
            // Arrange - create two regular players
            var p1 = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO { Username = "notAdmin1", Password = "Password123!" });
            var player1 = await p1.Content.ReadFromJsonAsync<BaseUser>();

            var p2 = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO { Username = "notAdmin2", Password = "Password123!" });
            var player2 = await p2.Content.ReadFromJsonAsync<BaseUser>();

            // Act - attempt to kick using non-admin player as adminId
            var response = await _client.PostAsync($"/api/clan/kick?kickeeId={player2!.Id}&adminId={player1!.Id}", null);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task GetMembers_ExistingClan_ReturnsOk()
        {
            // Arrange - create admin, clan and two players who join
            var adminRegister = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "membersAdmin",
                Password = "Password123!"
            });
            var admin = await adminRegister.Content.ReadFromJsonAsync<BaseUser>();
            await _client.PostAsJsonAsync($"/api/auth/elevate-to-admin?Id={admin!.Id}", new { });
            await _client.PostAsync($"/api/clan/create?clanName=MembersClan&userId={admin.Id}", null);

            var playerAReg = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO { Username = "memberA", Password = "Password123!" });
            var playerA = await playerAReg.Content.ReadFromJsonAsync<BaseUser>();
            var playerBReg = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO { Username = "memberB", Password = "Password123!" });
            var playerB = await playerBReg.Content.ReadFromJsonAsync<BaseUser>();

            var clanResponse = await _client.GetAsync("/api/clan/getclanbyname/MembersClan");
            var clan = await clanResponse.Content.ReadFromJsonAsync<Clan>();

            await _client.PostAsync($"/api/clan/join/{clan!.Id}?userId={playerA!.Id}", null);
            await _client.PostAsync($"/api/clan/join/{clan.Id}?userId={playerB!.Id}", null);

            // Act
            var response = await _client.GetAsync($"/api/clan/getmembers/{clan.Id}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var members = await response.Content.ReadFromJsonAsync<List<BaseUser>>();
            Assert.That(members, Is.Not.Null);
            Assert.That(members!.Count, Is.EqualTo(2));
            var usernames = members.Select(m => m.Username).ToList();
            Assert.That(usernames, Does.Contain("memberA"));
            Assert.That(usernames, Does.Contain("memberB"));
        }
    }
}
