using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using TriviaBackend.Controllers;
using TriviaBackend.Hubs;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Interfaces;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackendTests.Unit
{
    public class FriendshipControllerTests
    {
        private Mock<IFriendshipService> _mockFriendshipService;
        private Mock<IPresenceService> _mockPresenceService;
        private Mock<IHubContext<GameHub>> _mockHubContext;
        private Mock<IHubClients> _mockClients;
        private Mock<IClientProxy> _mockClientProxy;
        private FriendshipController _controller;

        [SetUp]
        public void Setup()
        {
            _mockFriendshipService = new Mock<IFriendshipService>();
            _mockPresenceService = new Mock<IPresenceService>();
            _mockHubContext = new Mock<IHubContext<GameHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClientProxy
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _controller = new FriendshipController(
                _mockFriendshipService.Object,
                _mockPresenceService.Object,
                _mockHubContext.Object);
        }
        [Test]
        public async Task SendRequest_ServiceReturnsNull_ReturnsConflict()
        {
            _mockFriendshipService
                .Setup(s => s.SendRequestAsync("user1", "user2"))
                .ReturnsAsync((FriendshipRequest?)null);

            var result = await _controller.SendRequest(new SendFriendRequestDTO("user1", "user2"));
            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }
        [Test]
        public async Task SendRequest_ServiceReturnsFriendship_ReturnsOk()
        {
            _mockFriendshipService
                .Setup(s => s.SendRequestAsync("user1", "user2"))
                .ReturnsAsync(new FriendshipRequest { Id = 1, RequesterId = "user1", AddresseeId = "user2" });

            var result = await _controller.SendRequest(new SendFriendRequestDTO("user1", "user2"));
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task RespondToRequest_AcceptSucceeds_ReturnsOk()
        {
            _mockFriendshipService
                .Setup(s => s.GetPendingRequestsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<FriendRequestEntry>());

            _mockFriendshipService
                .Setup(s => s.AcceptRequestAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            var result = await _controller.RespondToRequest(new RespondFriendRequestDTO("user1", 1, true));

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task RespondToRequest_AcceptFails_ReturnsBadRequest()
        {
            _mockFriendshipService
                .Setup(s => s.GetPendingRequestsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<FriendRequestEntry>());

            _mockFriendshipService
                .Setup(s => s.AcceptRequestAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var result = await _controller.RespondToRequest(new RespondFriendRequestDTO("user1", 1, true));

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RespondToRequest_DeclineFails_ReturnsBadRequest()
        {
            _mockFriendshipService
                .Setup(s => s.GetPendingRequestsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<FriendRequestEntry>());

            _mockFriendshipService
                .Setup(s => s.DeclineRequestAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var result = await _controller.RespondToRequest(new RespondFriendRequestDTO("user1", 1, false));

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        [Test]
        public async Task RemoveFriend_FriendshipNotFound_ReturnsNotFound()
        {
            _mockFriendshipService
                .Setup(s => s.RemoveFriendAsync("user1", "user2"))
                .ReturnsAsync(false);

            var result = await _controller.RemoveFriend("user1", "user2");

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task RemoveFriend_FriendshipFound_ReturnsNoContent()
        {
            _mockFriendshipService
                .Setup(s => s.RemoveFriendAsync("user1", "user2"))
                .ReturnsAsync(true);

            var result = await _controller.RemoveFriend("user1", "user2");

            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }
        [Test]
        public async Task InviteToGame_FriendNotInFriendList_ReturnsBadRequest()
        {
            _mockFriendshipService
                .Setup(s => s.GetFriendsAsync("user1"))
                .ReturnsAsync(new List<FriendEntry>());

            var result = await _controller.InviteToGame("user1", "user2", "game1");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task InviteToGame_FriendNotOnline_ReturnsBadRequest()
        {
            _mockFriendshipService
                .Setup(s => s.GetFriendsAsync("user1"))
                .ReturnsAsync(new List<FriendEntry>
                {
                    new FriendEntry("user2", "User2", PlayerStatus.Offline, null)
                });

            var result = await _controller.InviteToGame("user1", "user2", "game1");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        [Test]
        public async Task InviteToGame_FriendOnline_ReturnsOk()
        {
            _mockFriendshipService
                .Setup(s => s.GetFriendsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<FriendEntry>
                {
            new FriendEntry("user2", "User2", PlayerStatus.Online, null)
                });

            var result = await _controller.InviteToGame("user1", "user2", "game1");

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }
        [Test]
        public void PingPresence_EmptyUserId_ReturnsBadRequest()
        {
            var result = _controller.PingPresence(new UserPresenceDTO(""));

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void PingPresence_ValidUserId_ReturnsOk()
        {
            var result = _controller.PingPresence(new UserPresenceDTO("user1"));

            Assert.That(result, Is.InstanceOf<OkResult>());
        }
        [Test]
        public async Task LogoutPresence_EmptyUserId_ReturnsBadRequest()
        {
            var result = await _controller.LogoutPresence(new UserPresenceDTO(""));

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task LogoutPresence_ValidUserId_ReturnsOk()
        {
            _mockFriendshipService
                .Setup(s => s.GetFriendsAsync("user1"))
                .ReturnsAsync(new List<FriendEntry>());

            var result = await _controller.LogoutPresence(new UserPresenceDTO("user1"));

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

    }
}
