using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record ProgressRequest(string? StudentName, int CourseId, int LessonId);

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this WebApplication app)
    {
        app.MapPost("/api/progress", (ProgressRequest request, ProgressService progress) =>
        {
            var error = progress.MarkLessonComplete(request.StudentName, request.CourseId, request.LessonId);
            return error is null ? Results.Ok(new { completed = true }) : Results.BadRequest(new { error });
        });

        app.MapGet("/api/progress", (string? studentName, ProgressService progress) =>
            progress.GetProgress(studentName));
    }
}
