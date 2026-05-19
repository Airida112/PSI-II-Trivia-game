namespace TriviaBackend.Models.Records
{
    public record GameAnswer(
        int PlayerId,
        int QuestionId,
        int SelectedOptionIndex,
        DateTime SubmissionTime
    );
}
