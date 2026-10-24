using LearnHub.Api.Reports;
using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public static class RevenueEndpoints
{
    public static void MapRevenueEndpoints(this WebApplication app)
    {
        app.MapGet("/api/instructors/revenue", (ShopService shop) => shop.GetInstructorRevenue());

        app.MapGet("/api/instructors/payouts", (string? month, PayoutReportBuilder builder) =>
        {
            if (string.IsNullOrWhiteSpace(month) ||
                !System.Text.RegularExpressions.Regex.IsMatch(month, @"^\d{4}-(0[1-9]|1[0-2])$"))
                return Results.BadRequest(new { error = "month must look like YYYY-MM." });

            var report = builder.Build(int.Parse(month[..4]), int.Parse(month[5..]));
            return Results.Ok(new
            {
                month = report.Month,
                rows = report.Rows.Select(r => new
                {
                    r.InstructorId,
                    r.Instructor,
                    r.Lines,
                    r.Gross,
                    r.Share
                }),
                totalGross = report.TotalGross,
                totalShare = report.TotalShare
            });
        });
    }
}
