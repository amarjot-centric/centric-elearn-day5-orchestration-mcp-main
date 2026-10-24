using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record BookmarkRequest(string? StudentName, int CourseId);

public static class BookmarkEndpoints
{
    public static void MapBookmarkEndpoints(this WebApplication app)
    {
        app.MapPost("/api/bookmarks", (BookmarkRequest request, BookmarkService bookmarks) =>
        {
            var (bookmark, error) = bookmarks.AddBookmark(request.StudentName, request.CourseId);
            if (bookmark is null)
                return error == "Already bookmarked."
                    ? Results.Conflict(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/api/bookmarks/{bookmark.Id}", bookmark);
        });

        app.MapDelete("/api/bookmarks", (string? studentName, int courseId, BookmarkService bookmarks) =>
            bookmarks.RemoveBookmark(studentName, courseId) ? Results.NoContent() : Results.NotFound());

        app.MapGet("/api/bookmarks", (string? studentName, BookmarkService bookmarks) =>
            bookmarks.GetBookmarks(studentName));
    }
}
