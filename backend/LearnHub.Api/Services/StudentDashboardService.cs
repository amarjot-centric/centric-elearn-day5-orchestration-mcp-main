using LearnHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class StudentDashboardService
{
    private readonly StudentDirectory _students;
    private readonly ProgressService _progress;
    private readonly ShopService _shop;
    private readonly AssignmentService _assignments;
    private readonly IClock _clock;

    public StudentDashboardService(StudentDirectory students, ProgressService progress, ShopService shop,
        AssignmentService assignments, IClock clock)
    {
        _students = students;
        _progress = progress;
        _shop = shop;
        _assignments = assignments;
        _clock = clock;
    }

    public StudentDashboard? Get(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return null;

        var now = _clock.UtcNow;
        return new StudentDashboard(
            student.FullName,
            _progress.GetProgress(student.FullName).Where(p => p.Percent < 100).ToList(),
            _progress.GetCertificates(student.FullName),
            _shop.GetOrders(student.FullName),
            _assignments.ForStudent(student.FullName)
                .Where(a => a.DueAt >= now && !a.Submitted)
                .Select(a => new UpcomingAssignment(a.AssignmentId, a.CourseId, a.CourseTitle, a.Title, a.DueAt, a.Submitted))
                .ToList());
    }
}
