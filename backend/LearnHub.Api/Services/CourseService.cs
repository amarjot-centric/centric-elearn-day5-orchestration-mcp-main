using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class CourseService
{
    private static readonly string[] Levels = { "Beginner", "Intermediate", "Advanced" };

    private readonly LearnHubDbContext _db;
    private readonly IClock _clock;

    public CourseService(LearnHubDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public List<CourseListItem> Search(string? search)
    {
        var query = _db.Courses.AsNoTracking().AsQueryable();
        var text = search?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            var lower = text.ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(lower));
        }

        var rows = query.OrderBy(c => c.Id)
            .Select(c => new { Course = c, Instructor = c.Instructor.FullName, LessonCount = c.Lessons.Count })
            .ToList();
        var ratings = Ratings();
        return rows.Select(r =>
        {
            ratings.TryGetValue(r.Course.Id, out var rating);
            return new CourseListItem(r.Course.Id, r.Course.Title, r.Course.ShortDescription, r.Course.Category,
                r.Instructor, r.Course.InstructorId, r.Course.Level, r.Course.Price, r.LessonCount,
                rating.Average, rating.Count);
        }).ToList();
    }

    public CourseDetail? Get(int id)
    {
        var course = _db.Courses.AsNoTracking()
            .Include(c => c.Instructor)
            .Include(c => c.Lessons)
            .Include(c => c.Prerequisites).ThenInclude(p => p.RequiresCourse)
            .FirstOrDefault(c => c.Id == id);
        if (course is null) return null;

        Ratings().TryGetValue(id, out var rating);
        return new CourseDetail(course.Id, course.Title, course.ShortDescription, course.Category,
            course.Instructor.FullName, course.InstructorId, course.Level, course.Price, rating.Average, rating.Count,
            course.Prerequisites.Select(p => new PrerequisiteView(p.RequiresCourseId, p.RequiresCourse.Title)).ToList(),
            course.Lessons.OrderBy(l => l.Position).ThenBy(l => l.Id)
                .Select(l => new LessonView(l.Id, l.Title, l.DurationMinutes, l.Content)).ToList());
    }

    private Dictionary<int, (double Average, int Count)> Ratings() =>
        _db.Reviews.GroupBy(r => r.CourseId)
            .Select(g => new { CourseId = g.Key, Average = g.Average(r => (double)r.Stars), Count = g.Count() })
            .ToList()
            .ToDictionary(x => x.CourseId, x => (Math.Round(x.Average, 2), x.Count));

    /// <summary>Adds a course (and its lessons). Returns the course, or an error text.</summary>
    public (Course? Course, string? Error) AddCourse(CourseDraft draft)
    {
        var lessons = draft.Lessons ?? new();
        if (string.IsNullOrWhiteSpace(draft.Title)) return (null, "title is required.");
        if (string.IsNullOrWhiteSpace(draft.ShortDescription)) return (null, "shortDescription is required.");
        if (string.IsNullOrWhiteSpace(draft.Category)) return (null, "category is required.");
        if (string.IsNullOrWhiteSpace(draft.Instructor)) return (null, "instructor is required.");
        var level = Levels.FirstOrDefault(l => string.Equals(l, draft.Level?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (level is null) return (null, "level must be Beginner, Intermediate or Advanced.");
        if (draft.Price < 0) return (null, "price cannot be below 0.");
        if (lessons.Count == 0) return (null, "At least one lesson is required.");
        if (lessons.Any(l => string.IsNullOrWhiteSpace(l.Title))) return (null, "Every lesson needs a title.");
        if (lessons.Any(l => l.DurationMinutes <= 0)) return (null, "Every lesson needs durationMinutes above 0.");

        var title = draft.Title.Trim();
        var lowerTitle = title.ToLower();
        if (_db.Courses.Any(c => c.Title.ToLower() == lowerTitle)) return (null, "A course with this title already exists.");

        var instructor = FindOrCreateInstructor(draft.Instructor.Trim());
        var now = _clock.UtcNow;
        var course = new Course
        {
            Title = title,
            ShortDescription = draft.ShortDescription.Trim(),
            Category = draft.Category.Trim(),
            Instructor = instructor,
            Level = level,
            Price = draft.Price,
            CreatedAt = now,
            UpdatedAt = now,
            Lessons = lessons.Select((l, i) => new Lesson
            {
                Position = i + 1,
                Title = l.Title!.Trim(),
                DurationMinutes = l.DurationMinutes,
                Content = l.Content?.Trim() ?? ""
            }).ToList()
        };
        _db.Courses.Add(course);
        _db.SaveChanges();
        return (course, null);
    }

    private Instructor FindOrCreateInstructor(string name)
    {
        var lower = name.ToLower();
        var existing = _db.Instructors.FirstOrDefault(i => i.FullName.ToLower() == lower);
        if (existing is not null) return existing;

        var slug = string.Join(".", lower.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var email = $"{slug}@instructors.learnhub.test";
        var n = 1;
        while (_db.Instructors.Any(i => i.Email == email)) email = $"{slug}{++n}@instructors.learnhub.test";
        return new Instructor { FullName = name, Email = email, Bio = "", JoinedAt = _clock.UtcNow };
    }
}
