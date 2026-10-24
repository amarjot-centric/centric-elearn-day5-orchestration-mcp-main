namespace LearnHub.Tests;

public class CourseAndProgressTests
{
    [Fact]
    public void Courses_HaveUniqueIds()
    {
        using var env = new TestEnv();
        var ids = env.Db.Courses.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("data")]
    [InlineData("DATA")]
    public void Search_IsCaseInsensitive(string search)
    {
        using var env = new TestEnv();
        Assert.Contains(env.Courses.Search(search), c => c.Title == "Data Analysis with SQL");
    }

    [Fact]
    public void Search_WithNoMatch_ReturnsNothing()
    {
        using var env = new TestEnv();
        Assert.Empty(env.Courses.Search("zzz-no-match"));
    }

    [Fact]
    public void Enroll_Works_ThenRejectsDuplicateIgnoringCase()
    {
        using var env = new TestEnv();
        var first = env.Enrollments.Enroll(1, "Asha");
        var second = env.Enrollments.Enroll(1, "ASHA");

        Assert.NotNull(first.Enrollment);
        Assert.Null(second.Enrollment);
        Assert.Equal("Already enrolled in this course.", second.Error);
    }

    [Fact]
    public void Enroll_RejectsEmptyNameAndUnknownCourse()
    {
        using var env = new TestEnv();
        Assert.Equal("studentName is required.", env.Enrollments.Enroll(1, "  ").Error);
        Assert.Equal("Course not found.", env.Enrollments.Enroll(999, "Asha").Error);
    }

    [Fact]
    public void Quiz_HasTwoQuestionsForEachLessonOfFirstThreeCourses()
    {
        using var env = new TestEnv();
        var lessons = env.Db.Courses.OrderBy(c => c.Id).Take(3).SelectMany(c => c.Lessons).ToList();
        Assert.Equal(11, lessons.Count);
        foreach (var lesson in lessons)
            Assert.Equal(2, env.Quizzes.GetQuiz(lesson.Id).Count);
    }

    [Fact]
    public void Quiz_AllCorrect_ScoresFullAndPasses()
    {
        using var env = new TestEnv();
        var answers = env.Quizzes.GetQuiz(1).Select(q => (q.Id, q.CorrectIndex));

        var result = env.Quizzes.ScoreQuiz(1, answers);

        Assert.Equal((2, 2, true), (result.Score, result.Total, result.Passed));
    }

    [Fact]
    public void Quiz_HalfCorrect_Passes_NoneCorrect_Fails()
    {
        using var env = new TestEnv();
        var qs = env.Quizzes.GetQuiz(1);
        var wrong = (qs[1].CorrectIndex + 1) % 4;

        var half = env.Quizzes.ScoreQuiz(1, new[] { (qs[0].Id, qs[0].CorrectIndex), (qs[1].Id, wrong) });
        var none = env.Quizzes.ScoreQuiz(1, new[] { (qs[0].Id, (qs[0].CorrectIndex + 1) % 4), (qs[1].Id, wrong) });

        Assert.Equal((1, 2, true), (half.Score, half.Total, half.Passed));
        Assert.Equal((0, 2, false), (none.Score, none.Total, none.Passed));
    }

    [Fact]
    public void Progress_RequiresEnrollment()
    {
        using var env = new TestEnv();
        Assert.Equal("Student is not enrolled in this course.", env.Progress.MarkLessonComplete("Asha", 1, 1));
    }

    [Fact]
    public void Progress_IsIdempotent()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha");

        Assert.Null(env.Progress.MarkLessonComplete("Asha", 1, 1));
        Assert.Null(env.Progress.MarkLessonComplete("asha", 1, 1));

        Assert.Single(env.Db.LessonProgress);
    }

    [Fact]
    public void Progress_Percent_IsCompletedOverTotal()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha"); // course 1 has 4 lessons
        env.Progress.MarkLessonComplete("Asha", 1, 1);

        var p = Assert.Single(env.Progress.GetProgress("Asha"));

        Assert.Equal(1, p.CompletedLessons);
        Assert.Equal(4, p.TotalLessons);
        Assert.Equal(25, p.Percent);
    }

    [Fact]
    public void Progress_RejectsLessonFromAnotherCourse()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha");
        Assert.Equal("Lesson not found in this course.", env.Progress.MarkLessonComplete("Asha", 1, 5));
    }
}
