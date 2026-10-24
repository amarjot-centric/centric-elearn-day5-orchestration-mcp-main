using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class BookmarkService
{
    private readonly LearnHubDbContext _db;
    private readonly StudentDirectory _students;
    private readonly IClock _clock;

    public BookmarkService(LearnHubDbContext db, StudentDirectory students, IClock clock)
    {
        _db = db;
        _students = students;
        _clock = clock;
    }

    /// <summary>Returns the new bookmark, or an error text.</summary>
    public (BookmarkCreated? Bookmark, string? Error) AddBookmark(string? studentName, int courseId)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
        if (!_db.Courses.Any(c => c.Id == courseId)) return (null, "Course not found.");
        var student = _students.GetOrCreate(name);
        if (_db.Bookmarks.Any(b => b.StudentId == student.Id && b.CourseId == courseId)) return (null, "Already bookmarked.");

        var bookmark = new Bookmark { StudentId = student.Id, CourseId = courseId, CreatedAt = _clock.UtcNow };
        _db.Bookmarks.Add(bookmark);
        _db.SaveChanges();
        return (new BookmarkCreated(bookmark.Id, student.FullName, courseId, bookmark.CreatedAt), null);
    }

    /// <summary>Returns true if a bookmark was removed.</summary>
    public bool RemoveBookmark(string? studentName, int courseId)
    {
        var student = _students.Find(studentName);
        if (student is null) return false;
        var bookmark = _db.Bookmarks.FirstOrDefault(b => b.StudentId == student.Id && b.CourseId == courseId);
        if (bookmark is null) return false;
        _db.Bookmarks.Remove(bookmark);
        _db.SaveChanges();
        return true;
    }

    public List<BookmarkView> GetBookmarks(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return new();
        var rows = _db.Bookmarks.AsNoTracking().Where(b => b.StudentId == student.Id).OrderBy(b => b.Id)
            .Select(b => new { b.Id, b.CourseId }).ToList();
        var ids = rows.Select(r => r.CourseId).ToList();
        var courses = _db.Courses.AsNoTracking().Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Title, c.ShortDescription, c.Category, Instructor = c.Instructor.FullName, c.Level, LessonCount = c.Lessons.Count })
            .ToDictionary(c => c.Id);
        return rows.Select(r =>
        {
            var c = courses[r.CourseId];
            return new BookmarkView(r.Id, c.Id, c.Title, c.ShortDescription, c.Category, c.Instructor, c.Level, c.LessonCount, student.FullName);
        }).ToList();
    }
}
