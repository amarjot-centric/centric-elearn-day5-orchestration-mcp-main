using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record EnrollRequest(int CourseId, string? StudentName);

public static class EnrollmentEndpoints
{
    public static void MapEnrollmentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/enrollments", (EnrollRequest request, EnrollmentService enrollments) =>
        {
            var (enrollment, error) = enrollments.EnrollFree(request.CourseId, request.StudentName);
            if (enrollment is null)
                return error == "Already enrolled in this course."
                    ? Results.Conflict(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/api/enrollments/{enrollment.Id}", enrollment);
        });

        app.MapGet("/api/enrollments", (string? studentName, EnrollmentService enrollments) =>
            enrollments.GetEnrollments(studentName));
    }
}
