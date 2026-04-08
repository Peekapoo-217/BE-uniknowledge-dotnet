using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.DTOs.Answer;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api")]
public class AnswersController : ControllerBase
{
    private readonly IAnswerService _answerService;

    public AnswersController(IAnswerService answerService)
    {
        _answerService = answerService;
    }

    [HttpGet("questions/{questionId}/answers")]
    public async Task<ActionResult> GetAnswers(int questionId, [FromQuery] int limit = 20, [FromQuery] string? after = null)
    {
        var answers = await _answerService.GetAnswersByQuestionIdAsync(questionId, limit, after);
        return Ok(answers);
    }

    [HttpGet("answers/{id}/code")]
    public async Task<ActionResult<string>> GetAnswerCode(int id)
    {
        var code = await _answerService.GetAnswerCodeAsync(id);
        if (code == null) return NotFound();
        return Ok(code);
    }

    [Authorize]
    [HttpPost("questions/{questionId}/answers")]
    public async Task<ActionResult<AnswerResponseDto>> CreateAnswer(int questionId, [FromBody] CreateAnswerDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var answer = await _answerService.CreateAnswerAsync(questionId, dto, userId);
        return CreatedAtAction(nameof(GetAnswers), new { questionId }, answer);
    }

    [Authorize]
    [HttpPut("answers/{id}")]
    public async Task<ActionResult<AnswerResponseDto>> UpdateAnswer(int id, [FromBody] UpdateAnswerDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var answer = await _answerService.UpdateAnswerAsync(id, dto, userId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found or unauthorized" });
        }

        return Ok(answer);
    }

    [Authorize]
    [HttpDelete("answers/{id}")]
    public async Task<ActionResult> DeleteAnswer(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _answerService.DeleteAnswerAsync(id, userId);

        if (!result)
        {
            return NotFound(new { message = "Answer not found or unauthorized" });
        }

        return NoContent();
    }

    [Authorize]
    [HttpPut("answers/{id}/accept")]
    public async Task<ActionResult> AcceptAnswer(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _answerService.AcceptAnswerAsync(id, userId);

        if (!result)
        {
            return NotFound(new { message = "Answer not found or unauthorized" });
        }

        return Ok(new { message = "Answer accepted successfully" });
    }
}

