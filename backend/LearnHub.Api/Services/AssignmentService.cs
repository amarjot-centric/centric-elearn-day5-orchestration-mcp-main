using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class AssignmentService
{
    private readonly LearnHubDbContext _db;
    private readonly StudentDirectory _students;
    private readonly IClock _clock;

    public AssignmentService(LearnHubDbContext db, StudentDirectory students, IClock clock)
    {
        _db = db;
        _students = students;
        _clock = clock;
    }

    public List<AssignmentView> ForCourse(int courseId) => Views(_db.Assignments.Where(a => a.CourseId == courseId));

    public List<AssignmentView> ForInstructor(int instructorId) =>
        Views(_db.Assignments.Where(a => a.Course.InstructorId == instructorId));

    private List<AssignmentView> Views(IQueryable<Assignment> query)
    {
        var rows = query.AsNoTracking().OrderBy(a => a.DueAt).ThenBy(a => a.Id)
            .Select(a => new { a.Id, a.CourseId, CourseTitle = a.Course.Title, a.Title, a.Description, a.DueAt }).ToList();
        var counts = _db.Submissions.GroupBy(s => s.AssignmentId).Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionary(x => x.Id, x => x.Count);
        return rows.Select(r => new AssignmentView(r.Id, r.CourseId, r.CourseTitle, r.Title, r.Description, r.DueAt,
            counts.TryGetValue(r.Id, out var n) ? n : 0)).ToList();
    }

    public (AssignmentView? Assignment, string? Error) Create(int courseId, string? title, string? description, DateTime? dueAt)
    {
        var course = _db.Courses.FirstOrDefault(c => c.Id == courseId);
        if (course is null) return (null, "Course not found.");
        if (string.IsNullOrWhiteSpace(title)) return (null, "title is required.");
        if (dueAt is null) return (null, "dueAt is required.");

        var assignment = new Assignment
        {
            CourseId = courseId, Title = title.Trim(), Description = description?.Trim() ?? "",
            DueAt = dueAt.Value, CreatedAt = _clock.UtcNow
        };
        _db.Assignments.Add(assignment);
        _db.SaveChanges();
        return (new AssignmentView(assignment.Id, courseId, course.Title, assignment.Title, assignment.Description, assignment.DueAt, 0), null);
    }

    /// <summary>A student submits text for an assignment of a course they are enrolled in. Sending again replaces an ungraded submission.</summary>
    public (SubmissionView? Submission, string? Error) Submit(int assignmentId, string? studentName, string? body)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
        if (string.IsNullOrWhiteSpace(body)) return (null, "body is required.");
        var assignment = _db.Assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment is null) return (null, "Assignment not found.");
        var student = _students.Find(name);
        if (student is null || !_db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == assignment.CourseId))
            return (null, "Student is not enrolled in this course.");

        var now = _clock.UtcNow;
        var submission = _db.Submissions.FirstOrDefault(s => s.AssignmentId == assignmentId && s.StudentId == student.Id);
        if (submission is null)
        {
            submission = new Submission { AssignmentId = assignmentId, StudentId = student.Id, Body = body.Trim(), SubmittedAt = now };
            _db.Submissions.Add(submission);
        }
        else
        {
            if (submission.Grade is not null) return (null, "This submission was already graded.");
            submission.Body = body.Trim();
            submission.SubmittedAt = now;
        }
        _db.SaveChanges();
        return (ToView(submission, student.FullName), null);
    }

    public List<SubmissionView> GetSubmissions(int assignmentId) =>
        _db.Submissions.AsNoTracking().Where(s => s.AssignmentId == assignmentId).OrderBy(s => s.SubmittedAt).ThenBy(s => s.Id)
            .Select(s => new SubmissionView(s.Id, s.AssignmentId, s.Student.FullName, s.Body, s.SubmittedAt, s.Grade, s.Feedback, s.GradedAt))
            .ToList();

    public (SubmissionView? Submission, string? Error) Grade(int submissionId, int? grade, string? feedback)
    {
        var submission = _db.Submissions.Include(s => s.Student).FirstOrDefault(s => s.Id == submissionId);
        if (submission is null) return (null, "Submission not found.");
        if (grade is null || grade < 0 || grade > 100) return (null, "grade must be between 0 and 100.");

        submission.Grade = grade;
        submission.Feedback = feedback?.Trim();
        submission.GradedAt = _clock.UtcNow;
        _db.SaveChanges();
        return (ToView(submission, submission.Student.FullName), null);
    }

    /// <summary>Every assignment in the student's enrolled courses, with their own submission if any.</summary>
    public List<MyAssignmentView> ForStudent(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return new();

        var courseIds = _db.Enrollments.Where(e => e.StudentId == student.Id).Select(e => e.CourseId).ToList();
        var assignments = _db.Assignments.AsNoTracking().Where(a => courseIds.Contains(a.CourseId))
            .OrderBy(a => a.DueAt).ThenBy(a => a.Id)
            .Select(a => new { a.Id, a.CourseId, CourseTitle = a.Course.Title, a.Title, a.Description, a.DueAt }).ToList();
        var mine = _db.Submissions.AsNoTracking().Where(s => s.StudentId == student.Id).ToList()
            .ToDictionary(s => s.AssignmentId);
        return assignments.Select(a =>
        {
            mine.TryGetValue(a.Id, out var s);
            return new MyAssignmentView(a.Id, a.CourseId, a.CourseTitle, a.Title, a.Description, a.DueAt,
                s is not null, s?.Grade, s?.Feedback, s?.Body);
        }).ToList();
    }

    private static SubmissionView ToView(Submission s, string studentName) =>
        new(s.Id, s.AssignmentId, studentName, s.Body, s.SubmittedAt, s.Grade, s.Feedback, s.GradedAt);
}
