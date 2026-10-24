using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class ReviewService
{
    private readonly LearnHubDbContext _db;
    private readonly StudentDirectory _students;
    private readonly IClock _clock;

    public ReviewService(LearnHubDbContext db, StudentDirectory students, IClock clock)
    {
        _db = db;
        _students = students;
        _clock = clock;
    }

    /// <summary>Returns the new review, or an error text.</summary>
    public (ReviewView? Review, string? Error) AddReview(int courseId, string? studentName, int stars, string? comment)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
        if (!_db.Courses.Any(c => c.Id == courseId)) return (null, "Course not found.");
        if (stars < 1 || stars > 5) return (null, "stars must be between 1 and 5.");
        var text = comment?.Trim() ?? "";
        if (text.Length > 1000) return (null, "comment must be 1000 characters or fewer.");

        var student = _students.GetOrCreate(name);
        if (_db.Reviews.Any(r => r.CourseId == courseId && r.StudentId == student.Id))
            return (null, "You have already reviewed this course.");

        var review = new Review { CourseId = courseId, StudentId = student.Id, Stars = stars, Comment = text, CreatedAt = _clock.UtcNow };
        _db.Reviews.Add(review);
        _db.SaveChanges();
        return (new ReviewView(review.Id, courseId, student.FullName, stars, text, review.CreatedAt), null);
    }

    public List<ReviewView> GetReviews(int courseId) =>
        _db.Reviews.AsNoTracking().Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Select(r => new ReviewView(r.Id, r.CourseId, r.Student.FullName, r.Stars, r.Comment, r.CreatedAt)).ToList();
}
