using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriviaBackend.Models.Enums;
using TriviaBackend.Services.Implementations;

namespace TriviaBackendTests.Unit
{
    internal class PresenceServiceTests
    {
        private PresenceService _service;

        [SetUp]
        public void Setup()
        {
            _service = new PresenceService();
        }
        [Test]
        public void GetStatus_UserNotConnected_ReturnsOffline()
        {

            var result = _service.GetStatus("userID");

            Assert.That(result, Is.EqualTo(PlayerStatus.Offline));
        }
        [Test]
        public void GetStatus_UserOnline_ReturnsOnline()
        {
            _service.SetOnline("userID", "connectionID");

            var result = _service.GetStatus("userID");

            Assert.That(result, Is.EqualTo(PlayerStatus.Online));
        }

        [Test]
        public void GetStatus_UserInGame_ReturnsInGame()
        {
            _service.SetOnline("userID", "connectionID");
            _service.SetInGame("userID", "gameID");

            var result = _service.GetStatus("userID");

            Assert.That(result, Is.EqualTo(PlayerStatus.InGame));
        }

        [Test]
        public void SetOnline_ThenSetOffline_ReturnsOffline()
        {
            _service.SetOnline("userID", "connectionID");
            _service.SetOffline("connectionID");

            var result = _service.GetStatus("userID");

            Assert.That(result, Is.EqualTo(PlayerStatus.Offline));
        }

    }
}
