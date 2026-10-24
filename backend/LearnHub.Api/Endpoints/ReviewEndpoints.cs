using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record ReviewRequest(string? StudentName, int Stars, string? Comment);

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        app.MapPost("/api/courses/{id:int}/reviews", (int id, ReviewRequest request, ReviewService reviews) =>
        {
            var (review, error) = reviews.AddReview(id, request.StudentName, request.Stars, request.Comment);
            if (review is null)
                return error switch
                {
                    "Course not found." => Results.NotFound(),
                    "You have already reviewed this course." => Results.Conflict(new { error }),
                    _ => Results.BadRequest(new { error })
                };

            return Results.Created($"/api/courses/{id}/reviews", review);
        });

        app.MapGet("/api/courses/{id:int}/reviews", (int id, ReviewService reviews, CourseService courses) =>
            courses.Get(id) is null ? Results.NotFound() : Results.Ok(reviews.GetReviews(id)));
    }
}
