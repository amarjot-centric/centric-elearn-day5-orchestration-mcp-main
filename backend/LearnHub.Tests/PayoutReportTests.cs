namespace LearnHub.Tests;

public class PayoutReportTests
{
    [Fact]
    public void Payouts_AreSeventyPercentOfTheMonthsSales_PerInstructor()
    {
        using var env = new TestEnv();
        env.AddToCart("Asha", 2, 3, 4);                // Liam 34, Sofia 25.50, Ken 25.50 after the bundle
        env.Shop.Checkout("Asha", null);
        env.Clock.Now = new DateTime(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc);
        env.AddToCart("Ben", 2);                        // bought in April
        env.Shop.Checkout("Ben", null);

        var march = env.Payouts.Build(2026, 3);

        Assert.Equal("2026-03", march.Month);
        Assert.Equal(new[] { "Ken Watanabe", "Liam Carter", "Sofia Alvarez" }, march.Rows.Select(r => r.Instructor));
        var liam = march.Rows.Single(r => r.Instructor == "Liam Carter");
        Assert.Equal((1, 34m, 23.80m), (liam.Lines, liam.Gross, liam.Share));
        Assert.Equal(85m, march.TotalGross);
        Assert.Equal(59.50m, march.TotalShare);

        var april = env.Payouts.Build(2026, 4);
        Assert.Equal(40m, Assert.Single(april.Rows).Gross);
        Assert.Empty(env.Payouts.Build(2026, 5).Rows);
    }
}
