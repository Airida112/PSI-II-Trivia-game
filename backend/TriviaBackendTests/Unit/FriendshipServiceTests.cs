using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations.DB;
using TriviaBackend.Services.Interfaces;

namespace TriviaBackendTests.Unit
{
    public class FriendshipServiceTests
    {
        private Mock<ITriviaDbContext> _mockContext;
        private Mock<IPresenceService> _mockPresence;
        private FriendshipService _service;

        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<ITriviaDbContext>();
            _mockPresence = new Mock<IPresenceService>();
            _service = new FriendshipService(_mockContext.Object, _mockPresence.Object);
        }

        [Test]
        public async Task AcceptRequestAsync_FriendshipNotFound_ReturnsFalse()
        {
            _mockContext.Setup(c => c.Friendships.FindAsync(1)).ReturnsAsync((FriendshipRequest?)null);

            var result = await _service.AcceptRequestAsync("user1", 1);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task AcceptRequestAsync_WrongAddressee_ReturnsFalse()
        {
            _mockContext.Setup(c => c.Friendships.FindAsync(1))
                .ReturnsAsync(new FriendshipRequest
                {
                    Id = 1,
                    RequesterId = "user1",
                    AddresseeId = "user2",
                    Status = FriendshipStatus.Pending
                });

            var result = await _service.AcceptRequestAsync("wronguser", 1);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task AcceptRequestAsync_ValidRequest_ReturnsTrue()
        {
            _mockContext.Setup(c => c.Friendships.FindAsync(1))
                .ReturnsAsync(new FriendshipRequest
                {
                    Id = 1,
                    RequesterId = "user1",
                    AddresseeId = "user2",
                    Status = FriendshipStatus.Pending
                });

            _mockContext .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _service.AcceptRequestAsync("user2", 1);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task DeclineRequestAsync_FriendshipNotFound_ReturnsFalse()
        {
            _mockContext.Setup(c => c.Friendships.FindAsync(1)).ReturnsAsync((FriendshipRequest?)null);

            var result = await _service.DeclineRequestAsync("user1", 1);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeclineRequestAsync_WrongAddressee_ReturnsFalse()
        {
            _mockContext.Setup(c => c.Friendships.FindAsync(1))
                .ReturnsAsync(new FriendshipRequest
                {
                    Id = 1,
                    RequesterId = "user1",
                    AddresseeId = "user2",
                    Status = FriendshipStatus.Pending
                });

            var result = await _service.DeclineRequestAsync("wronguser", 1);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeclineRequestAsync_ValidRequest_ReturnsTrue()
        {
            _mockContext.Setup(c => c.Friendships.FindAsync(1))
                .ReturnsAsync(new FriendshipRequest
                {
                    Id = 1,
                    RequesterId = "user1",
                    AddresseeId = "user2",
                    Status = FriendshipStatus.Pending
                });

            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _service.DeclineRequestAsync("user2", 1);
            Assert.That(result, Is.True);
        }
    }
}
