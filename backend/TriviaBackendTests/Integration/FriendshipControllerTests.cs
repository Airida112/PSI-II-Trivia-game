using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Records;

namespace TriviaBackendTests.Integration
{
    public class FriendshipControllerTests
    {
        private TestWebApplicationFactory _factory;
        private HttpClient _client;

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
        public async Task SendRequest_ValidUsers_ReturnsOk()
        {
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "sender",
                Password = "password123"
            });

            var options = new System.Text.Json.JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            var body = await registerResponse.Content.ReadAsStringAsync();
            var sender = System.Text.Json.JsonSerializer.Deserialize<BaseUser>(body, options);

            await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "receiver",
                Password = "password123"
            });

            var response = await _client.PostAsJsonAsync("/api/friendship/send", new SendFriendRequestDTO(sender!.Id, "receiver"));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        [Test]
        public async Task RespondToRequest_AcceptSucceeds_ReturnsOk()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            var senderBody = await (await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "sender2",
                Password = "password123"
            })).Content.ReadAsStringAsync();
            var sender = System.Text.Json.JsonSerializer.Deserialize<BaseUser>(senderBody, options);

            var receiverBody = await (await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "receiver2",
                Password = "password123"
            })).Content.ReadAsStringAsync();
            var receiver = System.Text.Json.JsonSerializer.Deserialize<BaseUser>(receiverBody, options);

            var sendBody = await (await _client.PostAsJsonAsync("/api/friendship/send", new SendFriendRequestDTO(sender!.Id, "receiver2"))).Content.ReadAsStringAsync();
            var sendResult = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(sendBody, options);
            var friendshipId = sendResult.GetProperty("friendshipId").GetInt32();

            var response = await _client.PostAsJsonAsync("/api/friendship/respond", new RespondFriendRequestDTO(receiver!.Id, friendshipId, true));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task RemoveFriend_ReturnsNoContent()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            var senderBody = await (await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "sender3",
                Password = "password123"
            })).Content.ReadAsStringAsync();
            var sender = System.Text.Json.JsonSerializer.Deserialize<BaseUser>(senderBody, options);

            var receiverBody = await (await _client.PostAsJsonAsync("/api/auth/register", new BaseUserDTO
            {
                Username = "receiver3",
                Password = "password123"
            })).Content.ReadAsStringAsync();
            var receiver = System.Text.Json.JsonSerializer.Deserialize<BaseUser>(receiverBody, options);

            var sendBody = await (await _client.PostAsJsonAsync("/api/friendship/send", new SendFriendRequestDTO(sender!.Id, "receiver3"))).Content.ReadAsStringAsync();
            var sendResult = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(sendBody, options);
            var friendshipId = sendResult.GetProperty("friendshipId").GetInt32();

            await _client.PostAsJsonAsync("/api/friendship/respond", new RespondFriendRequestDTO(receiver!.Id, friendshipId, true));

            var response = await _client.DeleteAsync($"/api/friendship/remove?userId={sender!.Id}&friendId={receiver!.Id}");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
    }
}
