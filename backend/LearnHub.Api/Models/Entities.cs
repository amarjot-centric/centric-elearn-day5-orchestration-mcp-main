namespace LearnHub.Api.Models;

public class Instructor
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Bio { get; set; } = "";
    public DateTime JoinedAt { get; set; }
    public List<Course> Courses { get; set; } = new();
}

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime JoinedAt { get; set; }
}

public class Course
{
    public int Id { get; set; }
    public int InstructorId { get; set; }
    public string Title { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Category { get; set; } = "";
    public string Level { get; set; } = "";
    public decimal Price { get; set; } // 0 = free
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Instructor Instructor { get; set; } = null!;
    public List<Lesson> Lessons { get; set; } = new();
    public List<CoursePrerequisite> Prerequisites { get; set; } = new();
}

public class CoursePrerequisite
{
    public int CourseId { get; set; }
    public int RequiresCourseId { get; set; }
    public Course RequiresCourse { get; set; } = null!;
}

public class Lesson
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int Position { get; set; }
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public string Content { get; set; } = "";
}

public class QuizQuestion
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public string Question { get; set; } = "";
    public string OptionsJson { get; set; } = "[]";
    public int CorrectIndex { get; set; }

    public List<string> Options =>
        System.Text.Json.JsonSerializer.Deserialize<List<string>>(OptionsJson) ?? new();
}

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

public class LessonProgress
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int LessonId { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class Bookmark
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Student Student { get; set; } = null!;
}

public class CartItem
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime AddedAt { get; set; }
    public Student Student { get; set; } = null!;
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DiscountType { get; set; } = "Percent"; // "Percent" or "Fixed"
    public decimal DiscountValue { get; set; }
    public DateTime ExpiresOn { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public string Category { get; set; } = ""; // empty = every course
}

public class Order
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? CouponCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal BundleDiscount { get; set; }
    public decimal CouponDiscount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Paid"; // "Paid" or "Refunded"
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Student Student { get; set; } = null!;
    public List<OrderLine> Lines { get; set; } = new();
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CourseId { get; set; }
    public decimal Price { get; set; }
    public decimal BundleDiscount { get; set; }
    public decimal CouponDiscount { get; set; }
    public decimal FinalPrice { get; set; }
    public Order Order { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

public class Refund
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime RefundedAt { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
}

public class Review
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public int Stars { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Student Student { get; set; } = null!;
}

public class Question
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Student Student { get; set; } = null!;
    public List<Answer> Answers { get; set; } = new();
}

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public int? StudentId { get; set; }
    public int? InstructorId { get; set; }
    public string Body { get; set; } = "";
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; }
    public Student? Student { get; set; }
    public Instructor? Instructor { get; set; }
}

public class Assignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Course Course { get; set; } = null!;
}

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string Body { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public int? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
