using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class EnrollmentService
{
    private readonly LearnHubDbContext _db;
    private readonly StudentDirectory _students;
    private readonly ProgressService _progress;
    private readonly IClock _clock;

    public EnrollmentService(LearnHubDbContext db, StudentDirectory students, ProgressService progress, IClock clock)
    {
        _db = db;
        _students = students;
        _progress = progress;
        _clock = clock;
    }

    public bool IsEnrolled(string studentName, int courseId)
    {
        var student = _students.Find(studentName);
        return student is not null && _db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == courseId);
    }

    /// <summary>Returns the new enrollment, or an error text.</summary>
    public (EnrollmentView? Enrollment, string? Error) Enroll(int courseId, string? studentName)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
        var course = _db.Courses.FirstOrDefault(c => c.Id == courseId);
        if (course is null) return (null, "Course not found.");
        var student = _students.GetOrCreate(name);
        if (_db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == courseId))
            return (null, "Already enrolled in this course.");

        var enrollment = new Enrollment { StudentId = student.Id, CourseId = courseId, EnrolledAt = _clock.UtcNow };
        _db.Enrollments.Add(enrollment);
        _db.SaveChanges();
        return (new EnrollmentView(enrollment.Id, courseId, course.Title, student.FullName, enrollment.EnrolledAt), null);
    }

    /// <summary>Direct enrollment is for free courses, once the learning path is done. Paid courses go through the cart.</summary>
    public (EnrollmentView? Enrollment, string? Error) EnrollFree(int courseId, string? studentName)
    {
        var course = _db.Courses.FirstOrDefault(c => c.Id == courseId);
        if (course is not null && course.Price > 0) return (null, "This course is paid. Add it to your cart and check out.");

        var student = _students.Find(studentName);
        if (course is not null)
        {
            var required = _db.CoursePrerequisites.Where(p => p.CourseId == courseId)
                .Select(p => new { p.RequiresCourseId, p.RequiresCourse.Title }).ToList();
            foreach (var req in required)
            {
                var done = student is not null && _progress.PercentFor(student.Id, req.RequiresCourseId) >= 100;
                if (!done) return (null, $"Finish {req.Title} before you enroll in this course.");
            }
        }
        return Enroll(courseId, studentName);
    }

    public List<EnrollmentView> GetEnrollments(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return new();
        return _db.Enrollments.AsNoTracking().Where(e => e.StudentId == student.Id).OrderBy(e => e.Id)
            .Select(e => new EnrollmentView(e.Id, e.CourseId, e.Course.Title, student.FullName, e.EnrolledAt)).ToList();
    }
}
