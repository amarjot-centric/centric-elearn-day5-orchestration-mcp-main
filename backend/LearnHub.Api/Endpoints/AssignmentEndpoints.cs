using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record AssignmentRequest(string? Title, string? Description, DateTime? DueAt);
public record SubmissionRequest(string? StudentName, string? Body);
public record GradeRequest(int? Grade, string? Feedback);

public static class AssignmentEndpoints
{
    public static void MapAssignmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/courses/{id:int}/assignments", (int id, AssignmentService assignments, CourseService courses) =>
            courses.Get(id) is null ? Results.NotFound() : Results.Ok(assignments.ForCourse(id)));

        app.MapPost("/api/courses/{id:int}/assignments", (int id, AssignmentRequest request, AssignmentService assignments) =>
        {
            var (assignment, error) = assignments.Create(id, request.Title, request.Description, request.DueAt);
            if (assignment is null)
                return error == "Course not found." ? Results.NotFound() : Results.BadRequest(new { error });
            return Results.Created($"/api/courses/{id}/assignments", assignment);
        });

        app.MapGet("/api/assignments", (string? studentName, AssignmentService assignments) =>
            assignments.ForStudent(studentName));

        app.MapPost("/api/assignments/{id:int}/submissions", (int id, SubmissionRequest request, AssignmentService assignments) =>
        {
            var (submission, error) = assignments.Submit(id, request.StudentName, request.Body);
            if (submission is null)
                return error switch
                {
                    "Assignment not found." => Results.NotFound(),
                    "This submission was already graded." => Results.Conflict(new { error }),
                    _ => Results.BadRequest(new { error })
                };
            return Results.Created($"/api/assignments/{id}/submissions", submission);
        });

        app.MapGet("/api/assignments/{id:int}/submissions", (int id, AssignmentService assignments) =>
            assignments.GetSubmissions(id));

        app.MapPut("/api/submissions/{id:int}/grade", (int id, GradeRequest request, AssignmentService assignments) =>
        {
            var (submission, error) = assignments.Grade(id, request.Grade, request.Feedback);
            if (submission is null)
                return error == "Submission not found." ? Results.NotFound() : Results.BadRequest(new { error });
            return Results.Ok(submission);
        });
    }
}
