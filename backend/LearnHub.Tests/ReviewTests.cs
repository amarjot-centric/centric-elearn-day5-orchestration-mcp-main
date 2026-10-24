namespace LearnHub.Tests;

public class ReviewTests
{
    private static TestEnv EnrolledEnv(params string[] names)
    {
        var env = new TestEnv();
        foreach (var n in names) env.Enrollments.Enroll(1, n);
        return env;
    }

    [Fact]
    public void Review_IsSaved_AndListedNewestFirst()
    {
        using var env = EnrolledEnv("Asha", "Ben");
        Assert.NotNull(env.Reviews.AddReview(1, "Asha", 5, "Loved it").Review);
        env.Clock.Now = TestData.Today.AddDays(1);
        Assert.NotNull(env.Reviews.AddReview(1, "Ben", 3, "Fine").Review);

        var list = env.Reviews.GetReviews(1);
        Assert.Equal(new[] { "Ben", "Asha" }, list.Select(r => r.StudentName));
        Assert.Equal("Loved it", list[1].Comment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Review_StarsMustBeOneToFive(int stars)
    {
        using var env = EnrolledEnv("Asha");
        Assert.Equal("stars must be between 1 and 5.", env.Reviews.AddReview(1, "Asha", stars, "x").Error);
    }

    [Fact]
    public void Review_RequiresNameAndKnownCourse()
    {
        using var env = EnrolledEnv("Asha");
        Assert.Equal("studentName is required.", env.Reviews.AddReview(1, " ", 4, "x").Error);
        Assert.Equal("Course not found.", env.Reviews.AddReview(99, "Asha", 4, "x").Error);
    }

    [Fact]
    public void Review_OnePerStudentPerCourse_IgnoringCase()
    {
        using var env = EnrolledEnv("Asha");
        env.Reviews.AddReview(1, "Asha", 4, "Good");
        Assert.Equal("You have already reviewed this course.", env.Reviews.AddReview(1, "ASHA", 2, "Again").Error);
    }

    [Fact]
    public void Course_ShowsAverageRatingAndCount()
    {
        using var env = EnrolledEnv("Asha", "Ben");
        env.Reviews.AddReview(1, "Asha", 5, "");
        env.Reviews.AddReview(1, "Ben", 4, "");

        var detail = env.Courses.Get(1)!;
        Assert.Equal(4.5, detail.AverageRating);
        Assert.Equal(2, detail.ReviewCount);
        Assert.Equal(0, env.Courses.Search("Cloud").First().ReviewCount);
        Assert.Equal(4.5, env.Courses.Search("C#").Single().AverageRating);
    }
}
