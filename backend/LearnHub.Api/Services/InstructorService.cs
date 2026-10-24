using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class InstructorService
{
    private const decimal ShareRate = 0.70m;

    private readonly LearnHubDbContext _db;

    public InstructorService(LearnHubDbContext db) => _db = db;

    public List<InstructorSummary> List() =>
        _db.Instructors.AsNoTracking().OrderBy(i => i.FullName)
            .Select(i => new InstructorSummary(i.Id, i.FullName, i.Bio, i.Courses.Count)).ToList();

    public InstructorDashboard? Dashboard(int instructorId)
    {
        var instructor = _db.Instructors.AsNoTracking().FirstOrDefault(i => i.Id == instructorId);
        if (instructor is null) return null;

        var courses = _db.Courses.AsNoTracking().Where(c => c.InstructorId == instructorId).OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.Title, LessonCount = c.Lessons.Count }).ToList();
        var courseIds = courses.Select(c => c.Id).ToList();

        var enrollments = _db.Enrollments.AsNoTracking().Where(e => courseIds.Contains(e.CourseId))
            .Select(e => new { e.StudentId, e.CourseId }).ToList();
        var doneCounts = _db.LessonProgress.AsNoTracking().Where(p => courseIds.Contains(p.CourseId))
            .GroupBy(p => new { p.StudentId, p.CourseId })
            .Select(g => new { g.Key.StudentId, g.Key.CourseId, Count = g.Count() })
            .ToDictionary(x => (x.StudentId, x.CourseId), x => x.Count);
        var reviews = _db.Reviews.AsNoTracking().Where(r => courseIds.Contains(r.CourseId))
            .Select(r => new { r.CourseId, r.Stars }).ToList();
        var lines = _db.OrderLines.AsNoTracking().Where(l => courseIds.Contains(l.CourseId))
            .Select(l => new { l.FinalPrice, l.Order.Status }).ToList();

        bool Completed(int studentId, int courseId, int lessonCount) =>
            lessonCount > 0 && doneCounts.TryGetValue((studentId, courseId), out var n) && n >= lessonCount;

        var stats = courses.Select(c =>
        {
            var mine = enrollments.Where(e => e.CourseId == c.Id).ToList();
            var done = mine.Count(e => Completed(e.StudentId, c.Id, c.LessonCount));
            var stars = reviews.Where(r => r.CourseId == c.Id).Select(r => r.Stars).ToList();
            return new CourseStats(c.Id, c.Title, mine.Count, stars.Count == 0 ? 0 : Math.Round(stars.Average(), 2), stars.Count,
                mine.Count == 0 ? 0 : Math.Round(100.0 * done / mine.Count, 1));
        }).ToList();

        var totalEnrollments = enrollments.Count;
        var completed = enrollments.Count(e => Completed(e.StudentId, e.CourseId, courses.First(c => c.Id == e.CourseId).LessonCount));
        var gross = lines.Sum(l => l.FinalPrice);
        var refunds = lines.Where(l => l.Status == "Refunded").Sum(l => l.FinalPrice);
        return new InstructorDashboard(instructor.Id, instructor.FullName, courses.Count, totalEnrollments,
            gross, refunds, Money.Round((gross - refunds) * ShareRate),
            reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => (double)r.Stars), 2), reviews.Count,
            totalEnrollments == 0 ? 0 : Math.Round(100.0 * completed / totalEnrollments, 1), stats);
    }
}
