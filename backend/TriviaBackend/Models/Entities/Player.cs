namespace TriviaBackend.Models.Entities
{
    public class Player : BaseUser, IComparable<Player>
    {
        public int Elo { get; set; }
        public int GamesPlayed { get; set; }
        public int TotalPoints { get; set; }

        public int CompareTo(Player? other)
        {
            if (other == null) return 1;

            int eloComparison = other.Elo.CompareTo(this.Elo);
            if (eloComparison != 0)
                return eloComparison;

            int gamesComparison = other.GamesPlayed.CompareTo(this.GamesPlayed);
            if (gamesComparison != 0)
                return gamesComparison;

            return this.Created.CompareTo(other.Created);
        }

        public override string ToString()
        {
            return $"{Username} - Elo: {Elo}, Games: {GamesPlayed}, Points: {TotalPoints}";
        }
    }
}