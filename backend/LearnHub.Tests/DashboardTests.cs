namespace LearnHub.Tests;

public class DashboardTests
{
    [Fact]
    public void InstructorDashboard_CountsEnrollmentsRevenueRatingAndCompletion()
    {
        using var env = new TestEnv();
        env.AddToCart("Asha", 2);
        env.Shop.Checkout("Asha", null);
        env.AddToCart("Ben", 2);
        var benOrder = env.Shop.Checkout("Ben", null).Order!.Id;
        env.Enrollments.Enroll(1, "Cara");
        env.CompleteLessons("Asha", 2, 100);          // Asha finishes course 2
        env.Reviews.AddReview(2, "Asha", 5, "Great");
        env.Reviews.AddReview(2, "Ben", 3, "Okay");
        env.Shop.RefundOrder(benOrder);                // Ben's 40 comes back and he leaves the course

        var d = env.Instructors.Dashboard(2)!;          // Liam Carter teaches course 2

        Assert.Equal("Liam Carter", d.Instructor);
        Assert.Equal(1, d.CourseCount);
        Assert.Equal(1, d.Enrollments);
        Assert.Equal(80m, d.GrossSales);
        Assert.Equal(40m, d.Refunds);
        Assert.Equal(28m, d.Revenue);                  // 70% of 40
        Assert.Equal(4.0, d.AverageRating);
        Assert.Equal(100.0, d.CompletionRate);
        Assert.Null(env.Instructors.Dashboard(99));
    }

    [Fact]
    public void InstructorDashboard_IsZeroForAnInstructorWithoutStudents()
    {
        using var env = new TestEnv();
        var d = env.Instructors.Dashboard(4)!;
        Assert.Equal((0, 0m, 0.0, 0.0), (d.Enrollments, d.Revenue, d.AverageRating, d.CompletionRate));
    }

    [Fact]
    public void StudentDashboard_ShowsProgressCertificatesOrdersAndDeadlines()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha");
        env.Enrollments.Enroll(5, "Asha");
        env.CompleteLessons("Asha", 5, 100);
        env.CompleteLessons("Asha", 1, 1);
        env.AddToCart("Asha", 4);
        env.Shop.Checkout("Asha", null);
        var soon = env.Assignments.Create(1, "Soon", "", TestData.Today.AddDays(3)).Assignment!;
        env.Assignments.Create(1, "Past", "", TestData.Today.AddDays(-3));
        var done = env.Assignments.Create(5, "Done", "", TestData.Today.AddDays(2)).Assignment!;
        env.Assignments.Submit(done.Id, "Asha", "finished");

        var d = env.Dashboards.Get("Asha")!;

        Assert.Equal(new[] { 1, 4 }, d.InProgress.Select(p => p.CourseId));
        Assert.Equal("Cloud Fundamentals", Assert.Single(d.Certificates).CourseTitle);
        Assert.Single(d.Orders);
        Assert.Equal(soon.Id, Assert.Single(d.UpcomingAssignments).AssignmentId);
        Assert.Null(env.Dashboards.Get("Nobody"));
    }
}
