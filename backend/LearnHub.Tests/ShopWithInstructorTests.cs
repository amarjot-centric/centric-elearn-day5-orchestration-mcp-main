using LearnHub.Api.Services;

namespace LearnHub.Tests;

// The shop and the instructor features must work together.
public class ShopWithInstructorTests
{
    private static CourseDraft Draft(decimal price = 0, string title = "Git Basics") => new(
        title, "Track changes with Git.", "Programming", "Ana Ruiz", "Beginner",
        new List<LessonDraft> { new("Commits", 10, null) }, price);

    [Fact]
    public void InstructorCourse_IsFreeByDefault_AndCanHaveAPrice()
    {
        using var env = new TestEnv();
        Assert.Equal(0m, env.Courses.AddCourse(Draft()).Course!.Price);

        var paid = env.Courses.AddCourse(Draft(25m, "Paid Git")).Course!;
        Assert.Equal(25m, paid.Price);
        Assert.NotNull(env.Shop.AddToCart("Asha", paid.Id).Item);
    }

    [Fact]
    public void InstructorCourse_RejectsNegativePrice()
    {
        using var env = new TestEnv();
        Assert.Equal("price cannot be below 0.", env.Courses.AddCourse(Draft(-1)).Error);
    }

    [Fact]
    public void Refund_KeepsBookmarks_AndRemovesTheCertificateRoute()
    {
        using var env = new TestEnv();
        env.Bookmarks.AddBookmark("Asha", 2);
        env.Shop.AddToCart("Asha", 2);
        var orderId = env.Shop.Checkout("Asha", null).Order!.Id;
        env.Shop.RefundOrder(orderId);

        Assert.Single(env.Bookmarks.GetBookmarks("Asha"));
        Assert.Empty(env.Progress.GetCertificates("Asha"));
        Assert.False(env.Enrollments.IsEnrolled("Asha", 2));
    }

    [Fact]
    public void FinishedCourse_CannotBeRefunded_SoItsCertificateStays()
    {
        using var env = new TestEnv();
        env.Shop.AddToCart("Asha", 4);
        var orderId = env.Shop.Checkout("Asha", null).Order!.Id;
        env.CompleteLessons("Asha", 4, 100);

        Assert.NotNull(env.Shop.RefundOrder(orderId).Error);
        Assert.Single(env.Progress.GetCertificates("Asha"));
    }
}
