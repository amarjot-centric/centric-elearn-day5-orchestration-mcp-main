using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/students/dashboard", (string? studentName, StudentDashboardService dashboards) =>
        {
            if (string.IsNullOrWhiteSpace(studentName))
                return Results.BadRequest(new { error = "studentName is required." });
            var dashboard = dashboards.Get(studentName);
            return dashboard is null ? Results.NotFound(new { error = "Student not found." }) : Results.Ok(dashboard);
        });
    }
}
