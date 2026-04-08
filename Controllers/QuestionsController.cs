using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Question;
using UniKnowledge.Models;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly AppDbContext _context;

    public QuestionsController(IQuestionService questionService, AppDbContext context)
    {
        _questionService = questionService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetQuestions(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? tagId,
        [FromQuery] string? status,
        [FromQuery] int limit = 20,
        [FromQuery] string? after = null)
    {
        var questions = await _questionService.GetQuestionsAsync(search, categoryId, tagId, status, limit, after);
        return Ok(questions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuestionResponseDto>> GetQuestion(int id)
    {
        var question = await _questionService.GetQuestionByIdAsync(id);
        
        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        // Increment view count
        await _questionService.IncrementViewCountAsync(id);

        return Ok(question);
    }

    [HttpGet("{id}/code")]
    public async Task<ActionResult<string>> GetQuestionCode(int id)
    {
        var code = await _questionService.GetQuestionCodeAsync(id);
        if (code == null) return NotFound();
        return Ok(code);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<QuestionResponseDto>> CreateQuestion([FromBody] CreateQuestionDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var question = await _questionService.CreateQuestionAsync(dto, userId);
        return CreatedAtAction(nameof(GetQuestion), new { id = question.QuestionId }, question);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<QuestionResponseDto>> UpdateQuestion(int id, [FromBody] UpdateQuestionDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var question = await _questionService.UpdateQuestionAsync(id, dto, userId);

        if (question == null)
        {
            return NotFound(new { message = "Question not found or unauthorized" });
        }

        return Ok(question);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQuestion(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _questionService.DeleteQuestionAsync(id, userId);

        if (!result)
        {
            return NotFound(new { message = "Question not found or unauthorized" });
        }

        return NoContent();
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SeedQuestions()
    {
        // Double check role
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Admin")
        {
            return StatusCode(403, new { message = "Only Admin users can seed questions" });
        }

        // Check if categories exist
        var categories = await _context.Categories.ToListAsync();
        if (!categories.Any())
        {
            return BadRequest(new { message = "Please seed categories first by calling POST /api/Categories/seed" });
        }

        // Check if tags exist
        var tags = await _context.Tags.ToListAsync();
        if (!tags.Any())
        {
            return BadRequest(new { message = "Please seed tags first by calling POST /api/Tags/seed" });
        }

        // Get admin user (assuming first user or admin user)
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
        if (adminUser == null)
        {
            return BadRequest(new { message = "No admin user found. Please create an admin user first." });
        }

        // Get category IDs
        var programmingCat = categories.FirstOrDefault(c => c.Slug == "programming");
        var webDevCat = categories.FirstOrDefault(c => c.Slug == "web-development");
        var databaseCat = categories.FirstOrDefault(c => c.Slug == "database");
        var dataScienceCat = categories.FirstOrDefault(c => c.Slug == "data-science");

        // Get tag IDs
        var csharpTag = tags.FirstOrDefault(t => t.TagName == "C#");
        var reactTag = tags.FirstOrDefault(t => t.TagName == "Reactjs");
        var nodeTag = tags.FirstOrDefault(t => t.TagName == "NodeJs");

        var questions = new List<Question>
        {
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = programmingCat?.CategoryId ?? categories[0].CategoryId,
                Title = "How to implement dependency injection in C#?",
                Content = "I'm learning about dependency injection in C# and ASP.NET Core. Can someone explain how to properly implement DI in my application? What are the best practices?",
                Status = "Open",
                ViewCount = 42,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = webDevCat?.CategoryId ?? categories[0].CategoryId,
                Title = "React hooks vs class components - which should I use?",
                Content = "I'm starting a new React project and wondering whether I should use functional components with hooks or stick with class components. What are the pros and cons of each approach?",
                Status = "Open",
                ViewCount = 128,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = databaseCat?.CategoryId ?? categories[0].CategoryId,
                Title = "SQL Server performance optimization tips",
                Content = "My SQL Server database queries are running slowly. What are some best practices for optimizing database performance? Should I add indexes? How do I identify bottlenecks?",
                Status = "Open",
                ViewCount = 87,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = webDevCat?.CategoryId ?? categories[0].CategoryId,
                Title = "Node.js authentication with JWT",
                Content = "I'm building a REST API with Node.js and Express. What's the best way to implement JWT authentication? Are there any security considerations I should be aware of?",
                Status = "Open",
                ViewCount = 156,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = programmingCat?.CategoryId ?? categories[0].CategoryId,
                Title = "Understanding LINQ in C#",
                Content = "Can someone explain LINQ and when to use it? I see it everywhere in C# code but I'm not sure when it's appropriate to use LINQ vs traditional loops.",
                Status = "Open",
                ViewCount = 201,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = webDevCat?.CategoryId ?? categories[0].CategoryId,
                Title = "How to prevent XSS attacks in React applications?",
                Content = "I'm concerned about security in my React app. What are the best practices to prevent Cross-Site Scripting (XSS) attacks? Does React provide any built-in protection?",
                Status = "Open",
                ViewCount = 93,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = dataScienceCat?.CategoryId ?? categories[0].CategoryId,
                Title = "Machine learning model deployment best practices",
                Content = "I've trained a machine learning model and now need to deploy it to production. What are the best practices for deploying ML models? Should I use Docker? What about scaling?",
                Status = "Open",
                ViewCount = 67,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Question
            {
                UserId = adminUser.UserId,
                CategoryId = programmingCat?.CategoryId ?? categories[0].CategoryId,
                Title = "Async/Await in C# - Common pitfalls",
                Content = "I keep running into issues with async/await in my C# code. What are some common mistakes developers make with async programming and how can I avoid them?",
                Status = "Open",
                ViewCount = 234,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            }
        };

        _context.Questions.AddRange(questions);
        await _context.SaveChangesAsync();

        // Add tags to questions
        var questionTags = new List<QuestionTag>();
        
        if (csharpTag != null)
        {
            questionTags.Add(new QuestionTag { QuestionId = questions[0].QuestionId, TagId = csharpTag.TagId });
            questionTags.Add(new QuestionTag { QuestionId = questions[4].QuestionId, TagId = csharpTag.TagId });
            questionTags.Add(new QuestionTag { QuestionId = questions[7].QuestionId, TagId = csharpTag.TagId });
        }

        if (reactTag != null)
        {
            questionTags.Add(new QuestionTag { QuestionId = questions[1].QuestionId, TagId = reactTag.TagId });
            questionTags.Add(new QuestionTag { QuestionId = questions[5].QuestionId, TagId = reactTag.TagId });
        }

        if (nodeTag != null)
        {
            questionTags.Add(new QuestionTag { QuestionId = questions[3].QuestionId, TagId = nodeTag.TagId });
        }

        if (questionTags.Any())
        {
            _context.QuestionTags.AddRange(questionTags);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            message = "Questions seeded successfully",
            count = questions.Count,
            questions = questions.Select(q => new
            {
                q.QuestionId,
                q.Title,
                q.CategoryId,
                q.ViewCount,
                q.CreatedAt
            })
        });
    }
}

