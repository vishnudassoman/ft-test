using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EFCoreTest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EFCoreTest.Services;

public class CodingTestService(AppDbContext db, ILogger<CodingTestService> logger) : ICodingTestService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<CodingTestService> _logger = logger;

    public async Task GeneratePostSummaryReportAsync(int maxItems)
    {
        // Task placeholder:
        // - Emit REPORT_START, then up to `maxItems` lines prefixed with "POST_SUMMARY|" and
        //   finally REPORT_END. Each summary line must include PostId|AuthorName|CommentCount|LatestCommentAuthor.
        // - Method must be read-only and efficient for large datasets;
        // Implement the method body in the assessment; do not change the signature.
        IQueryable<Post> posts = _db.Posts.AsNoTracking().OrderByDescending(p => p.Id);
        var postsProjection = await posts.Select(p => new
        {
            PostId = p.Id,
            AuthorName = p.Author!.Name,
            CommentCount = p.Comments.Count,
            LatestCommentAuthor = p.Comments.OrderByDescending(c => c.CreatedAt).Select(c => c.Author != null ? c.Author.Name : null).FirstOrDefault(),
        }
        )
        .Select(x => $"POST_SUMMARY|{x.PostId}|{x.AuthorName}|{x.CommentCount}|{x.LatestCommentAuthor}")
        .Take(maxItems)
        .ToListAsync();
        if(postsProjection != null && postsProjection.Count >= 0)
        {
            postsProjection.Insert(0, "REPORT_START");
            postsProjection.Add("REPORT_END");
        }
        _logger.LogInformation(string.Join("\n", postsProjection!));
    }

    public async Task<IList<PostDto>> SearchPostSummariesAsync(string query, int maxResults = 50)
    {
        // Task placeholder:
        // - Return at most `maxResults` PostDto entries.
        // - Treat null/empty/whitespace query as no filter (return unfiltered results up to maxResults).
        // - Matching: case-insensitive substring in Title OR Content.
        // - Order by CreatedAt descending, project to PostDto, and avoid materializing full entities.
        // Implement the method body in the assessment; do not change the signature.

        IQueryable<Post> posts = _db.Posts.AsQueryable();
        // - Treat null/empty/whitespace query as no filter
        // - Matching: case-insensitive substring in Title OR Content.
        if (!string.IsNullOrWhiteSpace(query) && query.Length > 0)
        {
            posts = posts.Where(p=> EF.Functions.Like(p.Title, $"%{query}%") || EF.Functions.Like(p.Content, $"%{query}%") );
        }
        //Projection
        IQueryable<PostDto> postDtos = posts.Select(p => new PostDto { Id = p.Id, Title = p.Title, AuthorName = ( p.Author != null ? p.Author.Name : null), CreatedAt = p.CreatedAt, CommentCount = p.Comments.Count });
        //Take maxResults order by CreatedAt desc
        postDtos = postDtos.OrderByDescending(p => p.CreatedAt).Take(maxResults);

        return await postDtos.ToListAsync();
    }

    public async Task<IList<PostDto>> SearchPostSummariesAsync<TKey>(string query, int skip, int take, Expression<Func<PostDto, TKey>> orderBySelector, bool descending)
    {
        // Task placeholder:
        // - Server-side filter by `query` (null/empty => no filter), server-side ordering based on
        //   the provided DTO selector, then Skip/Take for paging. Project to PostDto and avoid
        //   per-row queries or client-side paging.
        // - Implementations may choose which selectors to support; unsupported selectors may
        //   be rejected by the grader.
        // Implement the method body in the assessment; do not change the signature.
        IQueryable<Post> posts = _db.Posts.AsQueryable();
        // - Server-side filter by `query` (null/empty => no filter)
        if (!string.IsNullOrEmpty(query))
        {
            posts = posts.Where(p => p.Title.Contains(query));
        }
        //Project to PostDto
        IQueryable<PostDto> postDtos = posts.Select(p => new PostDto { Id = p.Id, Title = p.Title, CreatedAt = p.CreatedAt });
        //server-side ordering based on the provided DTO selector
        if (descending)
        {
            postDtos = postDtos.OrderByDescending(orderBySelector);
        }
        else
        {
            postDtos = postDtos.OrderBy(orderBySelector);
        }

        //Skip/Take for paging
        postDtos = postDtos.Skip(skip).Take(take);
        return await postDtos.ToListAsync();
    }
}
