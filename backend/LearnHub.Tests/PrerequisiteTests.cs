using LearnHub.Api.Models;

namespace LearnHub.Tests;

public class PrerequisiteTests
{
    private static TestEnv WithPath()
    {
        var env = new TestEnv();
        // Cloud Fundamentals (5, free) must be finished before Intro to C# (1, free).
        env.Db.CoursePrerequisites.Add(new CoursePrerequisite { CourseId = 1, RequiresCourseId = 5 });
        env.Db.SaveChanges();
        return env;
    }

    [Fact]
    public void DirectEnroll_IsBlocked_UntilThePrerequisiteIsFinished()
    {
        using var env = WithPath();
        var blocked = env.Enrollments.EnrollFree(1, "Asha");
        Assert.Null(blocked.Enrollment);
        Assert.Equal("Finish Cloud Fundamentals before you enroll in this course.", blocked.Error);

        env.Enrollments.Enroll(5, "Asha");
        env.CompleteLessons("Asha", 5, 3); // 3 of 4 lessons
        Assert.NotNull(env.Enrollments.EnrollFree(1, "Asha").Error);

        env.CompleteLessons("Asha", 5, 4);
        Assert.NotNull(env.Enrollments.EnrollFree(1, "Asha").Enrollment);
    }

    [Fact]
    public void CourseDetail_ListsPrerequisites()
    {
        using var env = WithPath();
        var prereq = Assert.Single(env.Courses.Get(1)!.Prerequisites);
        Assert.Equal((5, "Cloud Fundamentals"), (prereq.Id, prereq.Title));
        Assert.Empty(env.Courses.Get(5)!.Prerequisites);
    }
}
