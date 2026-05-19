namespace TriviaBackend.Models.Entities
{
    public class Admin : BaseUser
    {
        public bool CanKickUsers { get; set; } = true;
        public bool CanManageContent { get; set; } = true;
    }
}
