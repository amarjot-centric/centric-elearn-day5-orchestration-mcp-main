using LearnHub.Api.Data;
using LearnHub.Api.Reports;
using LearnHub.Api.Models;
using LearnHub.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Tests;

public class FakeClock : IClock
{
    public DateTime Now { get; set; } = TestData.Today;
    public DateTime UtcNow => Now;
}

/// <summary>One SQLite in-memory database with the small test data set, and every service wired to it.</summary>
public sealed class TestEnv : IDisposable
{
    private readonly SqliteConnection _connection;

    public FakeClock Clock { get; } = new();
    public LearnHubDbContext Db { get; }
    public StudentDirectory Students { get; }
    public CourseService Courses { get; }
    public ProgressService Progress { get; }
    public EnrollmentService Enrollments { get; }
    public QuizService Quizzes { get; }
    public BookmarkService Bookmarks { get; }
    public ShopService Shop { get; }
    public ReviewService Reviews { get; }
    public QaService Qa { get; }
    public AssignmentService Assignments { get; }
    public InstructorService Instructors { get; }
    public StudentDashboardService Dashboards { get; }
    public PayoutReportBuilder Payouts { get; }

    public TestEnv()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Db = new LearnHubDbContext(new DbContextOptionsBuilder<LearnHubDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
        TestData.Seed(Db);

        Students = new StudentDirectory(Db, Clock);
        Courses = new CourseService(Db, Clock);
        Progress = new ProgressService(Db, Students, Clock);
        Enrollments = new EnrollmentService(Db, Students, Progress, Clock);
        Quizzes = new QuizService(Db);
        Bookmarks = new BookmarkService(Db, Students, Clock);
        Shop = new ShopService(Db, Students, Enrollments, Progress, Clock);
        Reviews = new ReviewService(Db, Students, Clock);
        Qa = new QaService(Db, Students, Clock);
        Assignments = new AssignmentService(Db, Students, Clock);
        Instructors = new InstructorService(Db);
        Dashboards = new StudentDashboardService(Students, Progress, Shop, Assignments, Clock);
        Payouts = new PayoutReportBuilder(Db);
    }

    public void AddToCart(string name, params int[] courseIds)
    {
        foreach (var id in courseIds) Assert.NotNull(Shop.AddToCart(name, id).Item);
    }

    public void CompleteLessons(string name, int courseId, int count)
    {
        foreach (var lesson in Db.Lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.Position).Take(count).ToList())
            Progress.MarkLessonComplete(name, courseId, lesson.Id);
    }

    public Coupon AddCoupon(string code, string type, decimal value, DateTime expiresOn, int maxUses, int usedCount = 0, decimal minOrder = 0)
    {
        var coupon = new Coupon
        {
            Code = code, DiscountType = type, DiscountValue = value, ExpiresOn = expiresOn,
            MaxUses = maxUses, UsedCount = usedCount, MinOrderAmount = minOrder
        };
        Db.Coupons.Add(coupon);
        Db.SaveChanges();
        return coupon;
    }

    public int CouponUses(string code) => Db.Coupons.AsNoTracking().First(c => c.Code == code).UsedCount;

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
