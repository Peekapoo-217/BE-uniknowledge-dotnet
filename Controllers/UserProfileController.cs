using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.DTOs.User;
using UniKnowledge.DTOs.Question;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UserProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _userProfileService.GetUserProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(profile);
    }

    [HttpGet("me/questions")]
    public async Task<ActionResult> GetMyQuestions([FromQuery] int limit = 20, [FromQuery] string? after = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var questions = await _userProfileService.GetUserQuestionsAsync(userId, limit, after);
        return Ok(questions);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var profile = await _userProfileService.UpdateProfileAsync(userId, dto);

            if (profile == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("me/change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var result = await _userProfileService.ChangePasswordAsync(userId, dto);

            if (!result)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> GetUserProfile(int id)
    {
        var profile = await _userProfileService.GetUserProfileAsync(id);

        if (profile == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(profile);
    }

    [HttpGet("{id}/questions")]
    [AllowAnonymous]
    public async Task<ActionResult> GetUserQuestions(int id, [FromQuery] int limit = 20, [FromQuery] string? after = null)
    {
        var questions = await _userProfileService.GetUserQuestionsAsync(id, limit, after);
        return Ok(questions);
    }


    [HttpPost("me/upload-avatar")]
    public async Task<ActionResult<UserProfileDto>> UploadAvatar(IFormFile file)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var profile = await _userProfileService.UploadAvatarAsync(userId, file);

            if (profile == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<UserProfileDto>>> SearchUsers([FromQuery] string? search = null, [FromQuery] int? limit = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var searchTerm = search ?? string.Empty;
        var limitValue = limit ?? 20;
        var users = await _userProfileService.SearchUsersAsync(searchTerm, userId, limitValue);
        return Ok(users);
    }
}