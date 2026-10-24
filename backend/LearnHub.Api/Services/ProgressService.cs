using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class ProgressService
{
    private readonly LearnHubDbContext _db;
    private readonly StudentDirectory _students;
    private readonly IClock _clock;

    public ProgressService(LearnHubDbContext db, StudentDirectory students, IClock clock)
    {
        _db = db;
        _students = students;
        _clock = clock;
    }

    /// <summary>Marks a lesson done. Safe to call twice. Returns an error text or null.</summary>
    public string? MarkLessonComplete(string? studentName, int courseId, int lessonId)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return "studentName is required.";
        if (!_db.Courses.Any(c => c.Id == courseId)) return "Course not found.";
        if (!_db.Lessons.Any(l => l.Id == lessonId && l.CourseId == courseId)) return "Lesson not found in this course.";
        var student = _students.Find(name);
        if (student is null || !_db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == courseId))
            return "Student is not enrolled in this course.";

        if (!_db.LessonProgress.Any(p => p.StudentId == student.Id && p.LessonId == lessonId))
        {
            _db.LessonProgress.Add(new LessonProgress
            {
                StudentId = student.Id, CourseId = courseId, LessonId = lessonId, CompletedAt = _clock.UtcNow
            });
            _db.SaveChanges();
        }
        return null;
    }

    public List<ProgressItem> GetProgress(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return new();

        var enrollments = _db.Enrollments.AsNoTracking().Where(e => e.StudentId == student.Id)
            .OrderBy(e => e.Id).Select(e => new { e.CourseId, e.Course.Title }).ToList();
        var done = _db.LessonProgress.AsNoTracking().Where(p => p.StudentId == student.Id)
            .Select(p => new { p.CourseId, p.LessonId }).ToList();
        var totals = _db.Lessons.GroupBy(l => l.CourseId).Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.CourseId, x => x.Count);

        return enrollments.Select(e =>
        {
            var ids = done.Where(p => p.CourseId == e.CourseId).Select(p => p.LessonId).ToList();
            totals.TryGetValue(e.CourseId, out var total);
            var percent = total == 0 ? 0 : (int)Math.Round(100.0 * ids.Count / total);
            return new ProgressItem(e.CourseId, e.Title, ids.Count, total, percent, ids);
        }).ToList();
    }

    public double PercentFor(int studentId, int courseId)
    {
        var total = _db.Lessons.Count(l => l.CourseId == courseId);
        if (total == 0) return 0;
        var done = _db.LessonProgress.Count(p => p.StudentId == studentId && p.CourseId == courseId);
        return 100.0 * done / total;
    }

    /// <summary>A certificate is earned when progress for a course is 100%.</summary>
    public List<CertificateView> GetCertificates(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return new();

        return GetProgress(studentName)
            .Where(p => p.TotalLessons > 0 && p.Percent == 100)
            .Select(p =>
            {
                var issuedOn = _db.LessonProgress.Where(x => x.StudentId == student.Id && x.CourseId == p.CourseId)
                    .Max(x => x.CompletedAt);
                return new CertificateView(p.CourseTitle, student.FullName, issuedOn,
                    $"LH-{p.CourseId:D3}-{student.FullName.ToUpperInvariant().Replace(" ", "")}");
            }).ToList();
    }
}
