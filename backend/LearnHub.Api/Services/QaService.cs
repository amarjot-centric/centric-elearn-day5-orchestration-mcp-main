using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class QaService
{
    private readonly LearnHubDbContext _db;
    private readonly StudentDirectory _students;
    private readonly IClock _clock;

    public QaService(LearnHubDbContext db, StudentDirectory students, IClock clock)
    {
        _db = db;
        _students = students;
        _clock = clock;
    }

    public (QuestionView? Question, string? Error) Ask(int courseId, string? studentName, string? body)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
        if (string.IsNullOrWhiteSpace(body)) return (null, "body is required.");
        if (!_db.Courses.Any(c => c.Id == courseId)) return (null, "Course not found.");

        var student = _students.GetOrCreate(name);
        var question = new Question { CourseId = courseId, StudentId = student.Id, Body = body.Trim(), CreatedAt = _clock.UtcNow };
        _db.Questions.Add(question);
        _db.SaveChanges();
        return (new QuestionView(question.Id, courseId, student.FullName, question.Body, question.CreatedAt, false, new()), null);
    }

    public List<QuestionView> GetQuestions(int courseId)
    {
        var questions = _db.Questions.AsNoTracking().Where(q => q.CourseId == courseId)
            .Include(q => q.Student)
            .Include(q => q.Answers).ThenInclude(a => a.Student)
            .Include(q => q.Answers).ThenInclude(a => a.Instructor)
            .OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.Id)
            .ToList();
        return questions.Select(q => new QuestionView(q.Id, q.CourseId, q.Student.FullName, q.Body, q.CreatedAt,
            q.Answers.Any(a => a.IsAccepted),
            q.Answers.OrderByDescending(a => a.IsAccepted).ThenBy(a => a.CreatedAt).ThenBy(a => a.Id)
                .Select(a => new AnswerView(a.Id,
                    a.Instructor != null ? a.Instructor.FullName : a.Student!.FullName,
                    a.Instructor != null ? "Instructor" : "Student",
                    a.Body, a.IsAccepted, a.CreatedAt)).ToList())).ToList();
    }

    /// <summary>An answer comes from a student (studentName) or from an instructor (instructorId).</summary>
    public (AnswerView? Answer, string? Error) Answer(int questionId, string? studentName, int? instructorId, string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return (null, "body is required.");
        var question = _db.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question is null) return (null, "Question not found.");

        var answer = new Answer { QuestionId = questionId, Body = body.Trim(), CreatedAt = _clock.UtcNow };
        string author;
        string role;
        if (instructorId is not null)
        {
            var instructor = _db.Instructors.FirstOrDefault(i => i.Id == instructorId);
            if (instructor is null) return (null, "Instructor not found.");
            answer.InstructorId = instructor.Id;
            author = instructor.FullName;
            role = "Instructor";
        }
        else
        {
            var name = studentName?.Trim();
            if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
            var student = _students.GetOrCreate(name);
            answer.StudentId = student.Id;
            author = student.FullName;
            role = "Student";
        }
        _db.Answers.Add(answer);
        _db.SaveChanges();
        return (new AnswerView(answer.Id, author, role, answer.Body, false, answer.CreatedAt), null);
    }

    /// <summary>Only the instructor who teaches the course can mark an answer as accepted. One per question.</summary>
    public (AnswerView? Answer, string? Error) Accept(int answerId, int instructorId)
    {
        var answer = _db.Answers.Include(a => a.Student).Include(a => a.Instructor).FirstOrDefault(a => a.Id == answerId);
        if (answer is null) return (null, "Answer not found.");
        var question = _db.Questions.First(q => q.Id == answer.QuestionId);
        var course = _db.Courses.First(c => c.Id == question.CourseId);
        if (course.InstructorId != instructorId) return (null, "Only the instructor of this course can accept an answer.");

        foreach (var other in _db.Answers.Where(a => a.QuestionId == answer.QuestionId && a.IsAccepted && a.Id != answerId))
            other.IsAccepted = false;
        answer.IsAccepted = true;
        _db.SaveChanges();
        return (new AnswerView(answer.Id,
            answer.Instructor != null ? answer.Instructor.FullName : answer.Student!.FullName,
            answer.Instructor != null ? "Instructor" : "Student",
            answer.Body, true, answer.CreatedAt), null);
    }
}
