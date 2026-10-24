namespace LearnHub.Api.Services;

public record CourseListItem(
    int Id, string Title, string ShortDescription, string Category, string Instructor, int InstructorId,
    string Level, decimal Price, int LessonCount, double AverageRating, int ReviewCount);

public record LessonView(int Id, string Title, int DurationMinutes, string Content);
public record PrerequisiteView(int Id, string Title);

public record CourseDetail(
    int Id, string Title, string ShortDescription, string Category, string Instructor, int InstructorId,
    string Level, decimal Price, double AverageRating, int ReviewCount,
    List<PrerequisiteView> Prerequisites, List<LessonView> Lessons);

public record LessonDraft(string? Title, int DurationMinutes, string? Content);

public record CourseDraft(
    string? Title, string? ShortDescription, string? Category, string? Instructor, string? Level,
    List<LessonDraft>? Lessons, decimal Price = 0);

public record EnrollmentView(int Id, int CourseId, string CourseTitle, string StudentName, DateTime EnrolledAt);
public record ProgressItem(int CourseId, string CourseTitle, int CompletedLessons, int TotalLessons, int Percent, List<int> CompletedLessonIds);
public record CertificateView(string CourseTitle, string StudentName, DateTime IssuedOn, string CertificateId);
public record QuizScore(int Score, int Total, bool Passed);

public record BookmarkView(int Id, int CourseId, string CourseTitle, string ShortDescription, string Category, string Instructor, string Level, int LessonCount, string StudentName);
public record BookmarkCreated(int Id, string StudentName, int CourseId, DateTime CreatedAt);
public record CartLineView(int CourseId, string Title, decimal Price, string Category, string Instructor);
public record CartItemCreated(string StudentName, int CourseId, DateTime AddedAt);

public record OrderLineView(int CourseId, string Title, string Instructor, decimal Price, decimal BundleDiscount, decimal CouponDiscount, decimal Final);
public record OrderView(
    int Id, string StudentName, DateTime CreatedAt, List<OrderLineView> Lines, string? CouponCode,
    decimal Subtotal, decimal BundleDiscount, decimal CouponDiscount, decimal Total, string Status);
public record InstructorRevenueRow(string Instructor, decimal GrossSales, decimal Refunds, decimal InstructorShare);

public record ReviewView(int Id, int CourseId, string StudentName, int Stars, string Comment, DateTime CreatedAt);

public record AnswerView(int Id, string AuthorName, string AuthorRole, string Body, bool IsAccepted, DateTime CreatedAt);
public record QuestionView(int Id, int CourseId, string StudentName, string Body, DateTime CreatedAt, bool HasAcceptedAnswer, List<AnswerView> Answers);

public record AssignmentView(int Id, int CourseId, string CourseTitle, string Title, string Description, DateTime DueAt, int SubmissionCount);
public record SubmissionView(int Id, int AssignmentId, string StudentName, string Body, DateTime SubmittedAt, int? Grade, string? Feedback, DateTime? GradedAt);
public record MyAssignmentView(int AssignmentId, int CourseId, string CourseTitle, string Title, string Description, DateTime DueAt, bool Submitted, int? Grade, string? Feedback, string? SubmissionBody);

public record InstructorSummary(int Id, string Name, string Bio, int CourseCount);
public record CourseStats(int CourseId, string Title, int Enrollments, double AverageRating, int ReviewCount, double CompletionRate);
public record InstructorDashboard(
    int InstructorId, string Instructor, int CourseCount, int Enrollments, decimal GrossSales, decimal Refunds,
    decimal Revenue, double AverageRating, int ReviewCount, double CompletionRate, List<CourseStats> Courses);

public record UpcomingAssignment(int AssignmentId, int CourseId, string CourseTitle, string Title, DateTime DueAt, bool Submitted);
public record StudentDashboard(
    string StudentName, List<ProgressItem> InProgress, List<CertificateView> Certificates,
    List<OrderView> Orders, List<UpcomingAssignment> UpcomingAssignments);
