using LearnHub.Api.Services;

namespace LearnHub.Tests;

public class AddCourseTests
{
    private static CourseDraft Draft(string title = "Git Basics") => new(
        title, "Track changes with Git.", "Programming", "Ana Ruiz", "beginner",
        new List<LessonDraft>
        {
            new("What is Git", 8, "Git saves versions."),
            new("Commits", 10, "A commit is a snapshot.")
        });

    [Fact]
    public void AddCourse_Works_AndAssignsNewUniqueIds()
    {
        using var env = new TestEnv();
        var oldMaxCourse = env.Db.Courses.Max(c => c.Id);
        var oldMaxLesson = env.Db.Lessons.Max(l => l.Id);

        var (course, error) = env.Courses.AddCourse(Draft());

        Assert.Null(error);
        Assert.Equal(oldMaxCourse + 1, course!.Id);
        Assert.Equal("Beginner", course.Level);
        Assert.Equal(new[] { oldMaxLesson + 1, oldMaxLesson + 2 }, course.Lessons.Select(l => l.Id));
        Assert.Contains(env.Courses.Search("git"), c => c.Id == course.Id);
    }

    [Fact]
    public void AddCourse_CreatesTheInstructorOnce()
    {
        using var env = new TestEnv();
        env.Courses.AddCourse(Draft("Git Basics"));
        env.Courses.AddCourse(Draft("Git Advanced"));

        Assert.Equal(1, env.Db.Instructors.Count(i => i.FullName == "Ana Ruiz"));
        Assert.Equal(2, env.Db.Courses.Count(c => c.Instructor.FullName == "Ana Ruiz"));
    }

    [Theory]
    [InlineData("title")]
    [InlineData("shortDescription")]
    [InlineData("category")]
    [InlineData("instructor")]
    public void AddCourse_RequiresTextFields(string field)
    {
        using var env = new TestEnv();
        var draft = Draft();
        if (field == "title") draft = draft with { Title = " " };
        if (field == "shortDescription") draft = draft with { ShortDescription = "" };
        if (field == "category") draft = draft with { Category = "" };
        if (field == "instructor") draft = draft with { Instructor = "" };

        var (course, error) = env.Courses.AddCourse(draft);

        Assert.Null(course);
        Assert.Equal($"{field} is required.", error);
    }

    [Fact]
    public void AddCourse_RejectsBadLevel()
    {
        using var env = new TestEnv();
        Assert.Equal("level must be Beginner, Intermediate or Advanced.", env.Courses.AddCourse(Draft() with { Level = "Expert" }).Error);
    }

    [Fact]
    public void AddCourse_RejectsMissingOrBadLessons()
    {
        using var env = new TestEnv();

        var none = Draft() with { Lessons = new List<LessonDraft>() };
        Assert.Equal("At least one lesson is required.", env.Courses.AddCourse(none).Error);

        var noTitle = Draft() with { Lessons = new List<LessonDraft> { new("", 5, "x") } };
        Assert.Equal("Every lesson needs a title.", env.Courses.AddCourse(noTitle).Error);

        var noTime = Draft() with { Lessons = new List<LessonDraft> { new("Intro", 0, "x") } };
        Assert.Equal("Every lesson needs durationMinutes above 0.", env.Courses.AddCourse(noTime).Error);
    }

    [Fact]
    public void AddCourse_RejectsDuplicateTitleIgnoringCase()
    {
        using var env = new TestEnv();
        Assert.Null(env.Courses.AddCourse(Draft("Git Basics")).Error);

        Assert.Equal("A course with this title already exists.", env.Courses.AddCourse(Draft("GIT BASICS")).Error);
    }
}
