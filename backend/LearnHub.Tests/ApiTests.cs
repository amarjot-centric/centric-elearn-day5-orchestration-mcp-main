using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LearnHub.Tests;

public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Courses_AreListed_AndSearchable()
    {
        var all = await Json(await _client.GetAsync("/api/courses"));
        Assert.Equal(6, all.GetArrayLength());
        Assert.Equal("Intro to C# Programming", all[0].GetProperty("title").GetString());
        Assert.Equal("Maya Rao", all[0].GetProperty("instructor").GetString());

        var found = await Json(await _client.GetAsync("/api/courses?search=sql"));
        Assert.Equal(1, found.GetArrayLength());
    }

    [Fact]
    public async Task CourseDetail_HasLessons_AndUnknownIs404()
    {
        var course = await Json(await _client.GetAsync("/api/courses/4"));
        Assert.Equal(5, course.GetProperty("lessons").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/courses/999")).StatusCode);
    }

    [Fact]
    public async Task EnrollAndReview_Flow()
    {
        var enroll = await _client.PostAsJsonAsync("/api/enrollments", new { courseId = 5, studentName = "Api Asha" });
        Assert.Equal(HttpStatusCode.Created, enroll.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync("/api/enrollments", new { courseId = 5, studentName = "api asha" })).StatusCode);

        var review = await _client.PostAsJsonAsync("/api/courses/5/reviews", new { studentName = "Api Asha", stars = 5, comment = "Clear" });
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync("/api/courses/5/reviews", new { studentName = "Api Asha", stars = 4, comment = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync("/api/courses/5/reviews", new { studentName = "Api Asha", stars = 9, comment = "" })).StatusCode);

        var list = await Json(await _client.GetAsync("/api/courses/5/reviews"));
        Assert.Equal(1, list.GetArrayLength());
        var detail = await Json(await _client.GetAsync("/api/courses/5"));
        Assert.Equal(5.0, detail.GetProperty("averageRating").GetDouble());
    }

    [Fact]
    public async Task CartCheckoutAndDashboard_Flow()
    {
        await _client.PostAsJsonAsync("/api/cart", new { studentName = "Api Ben", courseId = 2 });
        var preview = await Json(await _client.PostAsJsonAsync("/api/checkout/preview", new { studentName = "Api Ben", couponCode = "WELCOME10" }));
        Assert.Equal(36m, preview.GetProperty("total").GetDecimal());

        var order = await _client.PostAsJsonAsync("/api/checkout", new { studentName = "Api Ben", couponCode = "WELCOME10" });
        Assert.Equal(HttpStatusCode.Created, order.StatusCode);

        var dashboard = await Json(await _client.GetAsync("/api/instructors/2/dashboard"));
        Assert.True(dashboard.GetProperty("enrollments").GetInt32() >= 1);

        var payouts = await Json(await _client.GetAsync("/api/instructors/payouts?month=2026-03"));
        Assert.Equal("2026-03", payouts.GetProperty("month").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/instructors/payouts?month=March")).StatusCode);

        var student = await Json(await _client.GetAsync("/api/students/dashboard?studentName=Api%20Ben"));
        Assert.Equal(1, student.GetProperty("orders").GetArrayLength());
    }
}
