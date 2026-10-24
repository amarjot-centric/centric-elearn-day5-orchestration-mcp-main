using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public static class InstructorEndpoints
{
    public static void MapInstructorEndpoints(this WebApplication app)
    {
        app.MapGet("/api/instructors", (InstructorService instructors) => instructors.List());

        app.MapGet("/api/instructors/{id:int}/dashboard", (int id, InstructorService instructors) =>
        {
            var dashboard = instructors.Dashboard(id);
            return dashboard is null ? Results.NotFound() : Results.Ok(dashboard);
        });

        app.MapGet("/api/instructors/{id:int}/assignments", (int id, AssignmentService assignments) =>
            assignments.ForInstructor(id));
    }
}
