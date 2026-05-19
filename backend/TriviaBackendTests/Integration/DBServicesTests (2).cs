using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Enums;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Implementations.DB;

namespace TriviaBackendTests.Unit
{
    [TestFixture]
    public class DBServicesTests
    {
        private ServiceProvider _provider = null!;
        private IServiceScope _scope = null!;
        private TriviaDbContext _context = null!;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            var dbName = "TestDb_" + Guid.NewGuid();
            
            services.AddDbContext<TriviaDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<ITriviaDbContext, TriviaDbContext>();

            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<TriviaDbContext>();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _scope.Dispose();
            _provider.Dispose();
        }

        #region ClanService Tests

        [Test]
        public async Task ClanService_GetClanByIdAsync_ExistingClan_ReturnsClan()
        {
            // Arrange
            var service = new ClanService(_context);
            var clan = new Clan { Name = "TestClan" };
            _context.Clans.Add(clan);
            await _context.SaveChangesAsync();

            // Act
            var result = await service.GetClanByIdAsync(clan.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("TestClan"));
        }

        [Test]
        public async Task ClanService_GetClanByIdAsync_NonExistentClan_ReturnsNull()
        {
            var service = new ClanService(_context);
            var result = await service.GetClanByIdAsync(99999);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ClanService_GetClanByNameAsync_ExistingClan_ReturnsClan()
        {
            var service = new ClanService(_context);
            var clan = new Clan { Name = "NamedClan" };
            _context.Clans.Add(clan);
            await _context.SaveChangesAsync();

            var result = await service.GetClanByNameAsync("NamedClan");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("NamedClan"));
        }

        [Test]
        public async Task ClanService_GetClanByNameAsync_NonExistentClan_ReturnsNull()
        {
            var service = new ClanService(_context);
            var result = await service.GetClanByNameAsync("NonExistent");
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ClanService_CreateClanAsync_AddsClanToDatabase()
        {
            var service = new ClanService(_context);
            var clan = new Clan { Name = "NewClan" };

            await service.CreateClanAsync(clan);

            var result = await _context.Clans.FirstOrDefaultAsync(c => c.Name == "NewClan");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task ClanService_AddMemberToClanAsync_UpdatesUserAndClan()
        {
            var service = new ClanService(_context);
            var clan = new Clan { Name = "MemberClan", MemberCount = 0 };
            var user = new Player { Username = "TestPlayer", PasswordHash = "hash" };
            
            _context.Clans.Add(clan);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await service.AddMemberToClanAsync(clan, user);

            Assert.That(user.ClanId, Is.EqualTo(clan.Id));
            Assert.That(clan.MemberCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ClanService_RemoveMemberFromClanAsync_UpdatesUserAndClan()
        {
            var service = new ClanService(_context);
            var clan = new Clan { Name = "RemoveClan", MemberCount = 1 };
            var user = new Player { Username = "LeavingPlayer", PasswordHash = "hash", ClanId = 1 };
            
            _context.Clans.Add(clan);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            user.ClanId = clan.Id;
            await _context.SaveChangesAsync();

            await service.RemoveMemberFromClanAsync(clan, user);

            Assert.That(user.ClanId, Is.Null);
            Assert.That(clan.MemberCount, Is.EqualTo(0));
        }

        [Test]
        public async Task ClanService_RenameClanAsync_UpdatesClanName()
        {
            var service = new ClanService(_context);
            var clan = new Clan { Name = "OldName" };
            _context.Clans.Add(clan);
            await _context.SaveChangesAsync();

            await service.RenameClanAsync(clan, "NewName");

            var updated = await _context.Clans.FindAsync(clan.Id);
            Assert.That(updated!.Name, Is.EqualTo("NewName"));
        }

        [Test]
        public async Task ClanService_DeleteClanAsync_RemovesClan()
        {
            var service = new ClanService(_context);
            var clan = new Clan { Name = "DeleteMe" };
            _context.Clans.Add(clan);
            await _context.SaveChangesAsync();

            await service.DeleteClanAsync(clan);

            var result = await _context.Clans.FindAsync(clan.Id);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ClanService_GetAllClanMembersAsync_ReturnsMembers()
        {
            var service = new ClanService(_context);
            var clan = new Clan { Name = "MembersClan" };
            _context.Clans.Add(clan);
            await _context.SaveChangesAsync();

            var user1 = new Player { Username = "Member1", PasswordHash = "hash", ClanId = clan.Id };
            var user2 = new Player { Username = "Member2", PasswordHash = "hash", ClanId = clan.Id };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            var members = await service.GetAllClanMembersAsnyc(clan);

            Assert.That(members, Is.Not.Null);
            Assert.That(members!.Count, Is.EqualTo(2));
        }

        #endregion

        #region PlayerService Tests

        [Test]
        public async Task PlayerService_GetAllPlayersAsync_ReturnsOnlyPlayers()
        {
            var service = new PlayerService(_context);
            
            var player1 = new Player { Username = "Player1", PasswordHash = "hash", Elo = 1000 };
            var player2 = new Player { Username = "Player2", PasswordHash = "hash", Elo = 1200 };
            var admin = new Admin { Username = "Admin1", PasswordHash = "hash" };
            
            _context.Users.AddRange(player1, player2, admin);
            await _context.SaveChangesAsync();

            var result = await service.GetAllPlayersAsync();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(p => p is Player), Is.True);
        }

        [Test]
        public async Task PlayerService_GetPlayerByUsernameAsync_ExistingPlayer_ReturnsPlayer()
        {
            var service = new PlayerService(_context);
            var player = new Player { Username = "FindMe", PasswordHash = "hash", Elo = 1500 };
            _context.Users.Add(player);
            await _context.SaveChangesAsync();

            var result = await service.GetPlayerByUsernameAsync("FindMe");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Username, Is.EqualTo("FindMe"));
            Assert.That(result.Elo, Is.EqualTo(1500));
        }

        [Test]
        public async Task PlayerService_GetPlayerByUsernameAsync_NonExistentPlayer_ReturnsNull()
        {
            var service = new PlayerService(_context);
            var result = await service.GetPlayerByUsernameAsync("DoesNotExist");
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task PlayerService_UpdatePlayerAsync_UpdatesPlayerStats()
        {
            var service = new PlayerService(_context);
            var player = new Player { Username = "UpdateMe", PasswordHash = "hash", Elo = 1000, GamesPlayed = 5 };
            _context.Users.Add(player);
            await _context.SaveChangesAsync();

            player.Elo = 1100;
            player.GamesPlayed = 6;
            await service.UpdatePlayerAsync(player);

            var updated = await _context.Users.OfType<Player>().FirstOrDefaultAsync(p => p.Username == "UpdateMe");
            Assert.That(updated!.Elo, Is.EqualTo(1100));
            Assert.That(updated.GamesPlayed, Is.EqualTo(6));
        }

        #endregion

        #region QuestionsService Tests

        [Test]
        public async Task QuestionsService_GetQuestionByIdAsync_ExistingQuestion_ReturnsQuestion()
        {
            var service = new QuestionsService(_context);
            var question = new TriviaQuestion
            {
                QuestionText = "Test?",
                CorrectAnswerIndex = 0,
                Category = QuestionCategory.Science,
                Difficulty = DifficultyLevel.Easy,
                TimeLimit = 30
            };
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            var result = await service.GetQuestionByIdAsync(question.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.QuestionText, Is.EqualTo("Test?"));
        }

        [Test]
        public async Task QuestionsService_GetQuestionByIdAsync_NonExistentQuestion_ReturnsNull()
        {
            var service = new QuestionsService(_context);
            var result = await service.GetQuestionByIdAsync(99999);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task QuestionsService_AddQuestionAsync_AddsQuestionToDatabase()
        {
            var service = new QuestionsService(_context);
            var dto = new TriviaQuestionDTO(
                QuestionText: "New Question?",
                Answer1: "A",
                Answer2: "B",
                Answer3: "C",
                Answer4: "D",
                CorrectAnswerIndex: 1,
                Category: QuestionCategory.History,
                Difficulty: DifficultyLevel.Medium,
                TimeLimit: 25
            );

            var result = await service.AddQuestionAsync(dto);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.QuestionText, Is.EqualTo("New Question?"));
            Assert.That(result.AnswerOptions.Count, Is.EqualTo(4));
            Assert.That(result.CorrectAnswerIndex, Is.EqualTo(1));

            var dbQuestion = await _context.Questions.FindAsync(result.Id);
            Assert.That(dbQuestion, Is.Not.Null);
        }

        [Test]
        public async Task QuestionsService_DeleteQuestionByIdAsync_ExistingQuestion_RemovesQuestion()
        {
            var service = new QuestionsService(_context);
            var question = new TriviaQuestion
            {
                QuestionText = "Delete me?",
                CorrectAnswerIndex = 0,
                Category = QuestionCategory.Science,
                Difficulty = DifficultyLevel.Easy,
                TimeLimit = 30
            };
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            await service.DeleteQuestionByIdAsync(question.Id);

            var result = await _context.Questions.FindAsync(question.Id);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void QuestionsService_DeleteQuestionByIdAsync_NonExistentQuestion_DoesNotThrow()
        {
            var service = new QuestionsService(_context);
            Assert.DoesNotThrowAsync(async () => await service.DeleteQuestionByIdAsync(99999));
        }

        #endregion

        #region UserService Tests

        [Test]
        public async Task UserService_GetUserByIdAsync_ExistingUser_ReturnsUser()
        {
            var service = new UserService(_context);
            var user = new Player { Username = "UserById", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await service.GetUserByIdAsync(user.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Username, Is.EqualTo("UserById"));
        }

        [Test]
        public async Task UserService_GetUserByIdAsync_NonExistentUser_ReturnsNull()
        {
            var service = new UserService(_context);
            var result = await service.GetUserByIdAsync("nonexistent-id");
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task UserService_GetUserByUsernameAsync_ExistingUser_ReturnsUser()
        {
            var service = new UserService(_context);
            var user = new Admin { Username = "AdminUser", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await service.GetUserByUsernameAsync("AdminUser");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Username, Is.EqualTo("AdminUser"));
            Assert.That(result, Is.InstanceOf<Admin>());
        }

        [Test]
        public async Task UserService_GetUserByUsernameAsync_NonExistentUser_ReturnsNull()
        {
            var service = new UserService(_context);
            var result = await service.GetUserByUsernameAsync("DoesNotExist");
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task UserService_AddUserAsync_AddsUserToDatabase()
        {
            var service = new UserService(_context);
            var user = new Player { Username = "NewUser", PasswordHash = "hash", Elo = 1000 };

            await service.AddUserAsync(user);

            var result = await _context.Users.FirstOrDefaultAsync(u => u.Username == "NewUser");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task UserService_RemoveUserAsync_RemovesUserFromDatabase()
        {
            var service = new UserService(_context);
            var user = new Player { Username = "RemoveMe", PasswordHash = "hash" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await service.RemoveUserAsync(user);

            var result = await _context.Users.FirstOrDefaultAsync(u => u.Username == "RemoveMe");
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task UserService_AddAndRetrieveBothPlayerAndAdmin_WorksCorrectly()
        {
            var service = new UserService(_context);
            var player = new Player { Username = "PlayerUser", PasswordHash = "hash", Elo = 1000 };
            var admin = new Admin { Username = "AdminUser2", PasswordHash = "hash" };

            await service.AddUserAsync(player);
            await service.AddUserAsync(admin);

            var retrievedPlayer = await service.GetUserByUsernameAsync("PlayerUser");
            var retrievedAdmin = await service.GetUserByUsernameAsync("AdminUser2");

            Assert.That(retrievedPlayer, Is.InstanceOf<Player>());
            Assert.That(retrievedAdmin, Is.InstanceOf<Admin>());
        }

        #endregion
    }
}
