using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models.Entities;
using TriviaBackend.Models.Records;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Controllers
{
    /// <summary>
    /// Controller for managing trivia question editing
    /// </summary>
    /// <param name="_DBService"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class EditorController(IQuestionsService _DBService) : ControllerBase
    {
        [HttpGet("getquestion/{id}")]
        public async Task<ActionResult<TriviaQuestion>> GetQuestion(int id)
        {
            var question = await _DBService.GetQuestionByIdAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            return Ok(question);
        }

        [HttpPost("addquestion")]
        public async Task<ActionResult<string>> AddQuestion(TriviaQuestionDTO request)
        {
            if (request.CorrectAnswerIndex < 0 || request.CorrectAnswerIndex > 3)
            {
                return BadRequest("CorrectAnswerIndex must be between 0 and 3");
            }
            if (request.TimeLimit < 5 || request.TimeLimit > 1000)
            {
                return BadRequest("Time limit must be between 5 and 1000");
            }
            var created = await _DBService.AddQuestionAsync(request);
            return Ok(created);
        }

        [HttpDelete("deletequestion/{id}")]
        public async Task<ActionResult<string>> DeleteQuestion(int id)
        {
            var question = await _DBService.GetQuestionByIdAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            await _DBService.DeleteQuestionByIdAsync(id);
            return NoContent();
        }
    }
}
