using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TriviaBackend.Models.Enums;

namespace TriviaBackend.Models.Entities
{
    public class FriendshipRequest

    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RequesterId { get; set; } = string.Empty;

        [Required]
        public string AddresseeId { get; set; } = string.Empty;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

        public DateTime? RespondedAt { get; set; }

        [ForeignKey("RequesterId")]
        public BaseUser? Requester { get; set; }

        [ForeignKey("AddresseeId")]
        public BaseUser? Addressee { get; set; }
    }
}
