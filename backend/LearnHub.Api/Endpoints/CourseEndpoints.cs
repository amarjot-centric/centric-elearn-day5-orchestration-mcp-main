using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record LessonRequest(string? Title, int DurationMinutes, string? Content);

public record CourseRequest(
    string? Title, string? ShortDescription, string? Category,
    string? Instructor, string? Level, List<LessonRequest>? Lessons, decimal Price = 0);

public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this WebApplication app)
    {
        app.MapGet("/api/courses", (string? search, CourseService courses) => courses.Search(search));

        app.MapPost("/api/courses", (CourseRequest request, CourseService courses) =>
        {
            var draft = new CourseDraft(request.Title, request.ShortDescription, request.Category, request.Instructor,
                request.Level, request.Lessons?.Select(l => new LessonDraft(l.Title, l.DurationMinutes, l.Content)).ToList(),
                request.Price);
            var (course, error) = courses.AddCourse(draft);
            if (course is null)
                return error == "A course with this title already exists."
                    ? Results.Conflict(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/api/courses/{course.Id}", new
            {
                course.Id,
                course.Title,
                course.ShortDescription,
                course.Category,
                Instructor = course.Instructor.FullName,
                course.Level,
                LessonCount = course.Lessons.Count
            });
        });

        app.MapGet("/api/courses/{id:int}", (int id, CourseService courses) =>
        {
            var course = courses.Get(id);
            return course is null ? Results.NotFound() : Results.Ok(course);
        });
    }
}
