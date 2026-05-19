using System.ComponentModel.DataAnnotations;

namespace TriviaBackend.Models.Entities
{
    public class Clan
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MemberCount { get; set; } = 0;
    }
}
