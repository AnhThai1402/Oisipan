using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oishipan.DTOs;
using Oishipan.Models;

namespace Oishipan.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsArticlesController : ControllerBase
{
    private readonly OishipanContext _context;

    public NewsArticlesController(OishipanContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NewsArticleResponse>>> GetAll(
        [FromQuery] bool includeUnpublished = false)
    {
        var query = _context.NewsArticles.AsQueryable();
        if (!includeUnpublished)
        {
            query = query.Where(article =>
                article.IsPublished &&
                article.PublishedAt.HasValue &&
                article.PublishedAt <= DateTime.UtcNow);
        }

        var articlesData = await query
            .OrderByDescending(article => article.PublishedAt ?? article.CreatedAt)
            .ToListAsync();

        var articles = articlesData.Select(article => ToResponse(article)).ToList();

        return Ok(articles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NewsArticleResponse>> GetById(
        int id,
        [FromQuery] bool includeUnpublished = false)
    {
        var article = await _context.NewsArticles.FindAsync(id);
        if (article is null ||
            (!includeUnpublished &&
             (!article.IsPublished ||
              !article.PublishedAt.HasValue ||
              article.PublishedAt > DateTime.UtcNow)))
        {
            return NotFound(new { message = "Không tìm thấy bài viết." });
        }

        return Ok(ToResponse(article));
    }

    [HttpPost]
    public async Task<ActionResult<NewsArticleResponse>> Create(NewsArticleRequest request)
    {
        var now = DateTime.UtcNow;
        var article = new NewsArticle
        {
            Title = request.Title.Trim(),
            Summary = request.Summary.Trim(),
            Content = request.Content.Trim(),
            Image = CleanOptional(request.Image),
            IsPublished = request.IsPublished,
            PublishedAt = ResolvePublishedAt(request.IsPublished, request.PublishedAt, now),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.NewsArticles.Add(article);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = article.NewsArticleId }, ToResponse(article));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, NewsArticleRequest request)
    {
        var article = await _context.NewsArticles.FindAsync(id);
        if (article is null)
        {
            return NotFound(new { message = "Không tìm thấy bài viết." });
        }

        var now = DateTime.UtcNow;
        article.Title = request.Title.Trim();
        article.Summary = request.Summary.Trim();
        article.Content = request.Content.Trim();
        article.Image = CleanOptional(request.Image);
        article.IsPublished = request.IsPublished;
        article.PublishedAt = request.IsPublished
            ? article.PublishedAt ?? ResolvePublishedAt(true, request.PublishedAt, now)
            : article.PublishedAt;
        article.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _context.NewsArticles.FindAsync(id);
        if (article is null)
        {
            return NotFound(new { message = "Không tìm thấy bài viết." });
        }

        _context.NewsArticles.Remove(article);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime? ResolvePublishedAt(bool isPublished, DateTime? publishedAt, DateTime now)
    {
        if (!publishedAt.HasValue)
        {
            return isPublished ? now : null;
        }

        return ToUtc(publishedAt.Value);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static NewsArticleResponse ToResponse(NewsArticle article)
    {
        return new NewsArticleResponse
        {
            NewsArticleId = article.NewsArticleId,
            Title = article.Title,
            Summary = article.Summary,
            Content = article.Content,
            Image = article.Image,
            IsPublished = article.IsPublished,
            PublishedAt = article.PublishedAt.HasValue ? AsUtc(article.PublishedAt.Value) : null,
            CreatedAt = AsUtc(article.CreatedAt),
            UpdatedAt = AsUtc(article.UpdatedAt)
        };
    }
}
