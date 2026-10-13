using Microsoft.EntityFrameworkCore;
using MentorFlow.API.DTOs;

namespace MentorFlow.API.Services;
internal static class QueryPaging
{
    public static async Task<PagedResponse<TResponse>> ToPageAsync<TEntity, TResponse>(this IQueryable<TEntity> query, PageRequest request, Func<TEntity, TResponse> map, CancellationToken ct)
    {
        // Reject an excessive offset instead of allowing integer overflow.
        var offset = ((long)request.Page - 1) * request.PageSize;
        if (offset > int.MaxValue)
            throw new ApiException(StatusCodes.Status400BadRequest, "Page is too large.");
        var total = await query.CountAsync(ct);
        var items = await query.Skip((int)offset).Take(request.PageSize).ToListAsync(ct);
        return new PagedResponse<TResponse>(items.Select(map).ToList(), total, request.Page, request.PageSize);
    }
}
