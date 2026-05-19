using TriviaBackend.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TriviaBackend.Models.Entities
{
    public class TriviaQuestion
    {
        [Key]
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string Answer1 { get; set; } = string.Empty;
        public string Answer2 { get; set; } = string.Empty;
        public string Answer3 { get; set; } = string.Empty;
        public string Answer4 { get; set; } = string.Empty;

        [NotMapped]
        public string[] AnswerOptions
        {
            get => [Answer1, Answer2, Answer3, Answer4];
            set
            {
                Answer1 = value.Length > 0 ? value[0] : string.Empty;
                Answer2 = value.Length > 1 ? value[1] : string.Empty;
                Answer3 = value.Length > 2 ? value[2] : string.Empty;
                Answer4 = value.Length > 3 ? value[3] : string.Empty;
            }
        }
        public int CorrectAnswerIndex { get; set; }
        public QuestionCategory Category { get; set; }
        public DifficultyLevel Difficulty { get; set; }

        public int TimeLimit { get; set; } = 30;
        public int Points => (int)Difficulty * 10;

        public TriviaQuestion()
        {
            AnswerOptions = [];
        }
    }
}
