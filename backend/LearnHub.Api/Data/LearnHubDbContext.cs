using System.Text.RegularExpressions;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Data;

public class LearnHubDbContext : DbContext
{
    public LearnHubDbContext(DbContextOptions<LearnHubDbContext> options) : base(options) { }

    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgress => Set<LessonProgress>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Instructor>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Student>().HasIndex(x => x.Email).IsUnique();

        b.Entity<Course>().HasIndex(x => x.Title).IsUnique();
        b.Entity<Course>().HasOne(c => c.Instructor).WithMany(i => i.Courses).HasForeignKey(c => c.InstructorId);
        b.Entity<Course>().HasMany(c => c.Lessons).WithOne().HasForeignKey(l => l.CourseId);

        b.Entity<CoursePrerequisite>().HasKey(x => new { x.CourseId, x.RequiresCourseId });
        b.Entity<CoursePrerequisite>().HasOne<Course>().WithMany(c => c.Prerequisites).HasForeignKey(x => x.CourseId);
        b.Entity<CoursePrerequisite>().HasOne(x => x.RequiresCourse).WithMany().HasForeignKey(x => x.RequiresCourseId);

        b.Entity<QuizQuestion>().Ignore(q => q.Options);
        b.Entity<QuizQuestion>().HasOne<Lesson>().WithMany().HasForeignKey(q => q.LessonId);

        b.Entity<Enrollment>().HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
        b.Entity<Enrollment>().HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
        b.Entity<Enrollment>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);

        b.Entity<LessonProgress>().HasIndex(x => new { x.StudentId, x.LessonId }).IsUnique();
        b.Entity<LessonProgress>().HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId);
        b.Entity<LessonProgress>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId);
        b.Entity<LessonProgress>().HasOne<Lesson>().WithMany().HasForeignKey(x => x.LessonId);

        b.Entity<Bookmark>().HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
        b.Entity<Bookmark>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId);

        b.Entity<CartItem>().HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
        b.Entity<CartItem>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId);

        b.Entity<Coupon>().HasIndex(x => x.Code).IsUnique();

        b.Entity<Order>().HasOne(o => o.Student).WithMany().HasForeignKey(o => o.StudentId);
        b.Entity<Order>().HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId);
        b.Entity<OrderLine>().HasOne(l => l.Course).WithMany().HasForeignKey(l => l.CourseId);
        b.Entity<Refund>().HasOne<Order>().WithMany().HasForeignKey(r => r.OrderId);

        b.Entity<Review>().HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
        b.Entity<Review>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId);
        b.Entity<Review>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);

        b.Entity<Question>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId);
        b.Entity<Question>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.Entity<Answer>().HasOne<Question>().WithMany(q => q.Answers).HasForeignKey(x => x.QuestionId);
        b.Entity<Answer>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.Entity<Answer>().HasOne(x => x.Instructor).WithMany().HasForeignKey(x => x.InstructorId);

        b.Entity<Assignment>().HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
        b.Entity<Submission>().HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
        b.Entity<Submission>().HasOne(x => x.Assignment).WithMany().HasForeignKey(x => x.AssignmentId);
        b.Entity<Submission>().HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);

        // Table and column names are snake_case and match db/schema.sql.
        var tables = new Dictionary<Type, string>
        {
            [typeof(Instructor)] = "instructors", [typeof(Student)] = "students", [typeof(Course)] = "courses",
            [typeof(CoursePrerequisite)] = "course_prerequisites", [typeof(Lesson)] = "lessons",
            [typeof(QuizQuestion)] = "quiz_questions", [typeof(Enrollment)] = "enrollments",
            [typeof(LessonProgress)] = "lesson_progress", [typeof(Bookmark)] = "bookmarks",
            [typeof(CartItem)] = "cart_items", [typeof(Coupon)] = "coupons", [typeof(Order)] = "orders",
            [typeof(OrderLine)] = "order_lines", [typeof(Refund)] = "refunds", [typeof(Review)] = "reviews",
            [typeof(Question)] = "questions", [typeof(Answer)] = "answers", [typeof(Assignment)] = "assignments",
            [typeof(Submission)] = "submissions",
        };
        foreach (var entity in b.Model.GetEntityTypes())
        {
            entity.SetTableName(tables[entity.ClrType]);
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(Snake(property.Name));
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    property.SetPrecision(10);
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    property.SetScale(2);
            }
        }
    }

    private static string Snake(string name) =>
        Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}
