using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniKnowledge.DTOs.Tag;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SeedTags()
    {
        var result = await _tagService.SeedTagsAsync();

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            count = result.Count,
            tags = result.Tags
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TagResponseDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        var result = await _tagService.CreateTagAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(nameof(GetTag), new { id = result.Tag!.TagId }, result.Tag);
    }

    [HttpGet]
    public async Task<ActionResult<List<TagResponseDto>>> GetTags([FromQuery] string? search)
    {
        var tags = await _tagService.GetTagsAsync(search);
        return Ok(tags);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagResponseDto>> GetTag(int id)
    {
        var tag = await _tagService.GetTagByIdAsync(id);

        if (tag == null)
        {
            return NotFound(new { message = "Tag not found" });
        }

        return Ok(tag);
    }

    [HttpGet("popular")]
    public async Task<ActionResult<List<TagResponseDto>>> GetPopularTags([FromQuery] int limit = 20)
    {
        var tags = await _tagService.GetPopularTagsAsync(limit);
        return Ok(tags);
    }

    [HttpGet("{id}/questions")]
    public async Task<ActionResult> GetQuestionsByTag(int id, [FromQuery] int limit = 20, [FromQuery] string? after = null)
    {
        var result = await _tagService.GetQuestionsByTagAsync(id, limit, after);

        if (!result.Success)
        {
            return NotFound(new { message = result.Message });
        }

        return Ok(new
        {
            tagId = id,
            tagName = result.TagName,
            items = result.Items,
            pageInfo = result.PageInfo
        });
    }

    [HttpGet("suggest")]
    public async Task<ActionResult<List<TagResponseDto>>> SuggestTags([FromQuery] string? query, [FromQuery] int limit = 10)
    {
        var tags = await _tagService.SuggestTagsAsync(query, limit);
        return Ok(tags);
    }

    [HttpGet("trending")]
    public async Task<ActionResult<List<TagResponseDto>>> GetTrendingTags([FromQuery] int days = 7, [FromQuery] int limit = 20)
    {
        var tags = await _tagService.GetTrendingTagsAsync(days, limit);
        return Ok(tags);
    }

    [HttpPost("questions/filter")]
    public async Task<ActionResult> FilterQuestionsByTags([FromBody] TagFilterDto dto)
    {
        var result = await _tagService.FilterQuestionsByTagsAsync(dto);

        return Ok(new
        {
            items = result.Items,
            pageInfo = result.PageInfo
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<TagResponseDto>> UpdateTag(int id, [FromBody] UpdateTagDto dto)
    {
        var result = await _tagService.UpdateTagAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message == "Tag not found")
            {
                return NotFound(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        return Ok(result.Tag);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(int id)
    {
        var result = await _tagService.DeleteTagAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Tag not found")
            {
                return NotFound(new { message = result.Message });
            }
            return BadRequest(new { message = result.Message });
        }

        return NoContent();
    }
}
