using LearnHub.Api.Data;
using LearnHub.Api.Endpoints;
using LearnHub.Api.Reports;
using LearnHub.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// LEARNHUB_DB (an environment variable) wins over appsettings.json.
var connectionString = builder.Configuration["LEARNHUB_DB"]
    ?? builder.Configuration.GetConnectionString("LearnHub")
    ?? throw new InvalidOperationException("No connection string for LearnHub.");

builder.Services.AddDbContext<LearnHubDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0))));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<StudentDirectory>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<EnrollmentService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<BookmarkService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<QaService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<InstructorService>();
builder.Services.AddScoped<StudentDashboardService>();
builder.Services.AddScoped<PayoutReportBuilder>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

app.MapCourseEndpoints();
app.MapEnrollmentEndpoints();
app.MapQuizEndpoints();
app.MapProgressEndpoints();
app.MapCartEndpoints();
app.MapCheckoutEndpoints();
app.MapOrderEndpoints();
app.MapRevenueEndpoints();
app.MapBookmarkEndpoints();
app.MapCertificateEndpoints();
app.MapInstructorEndpoints();
app.MapReviewEndpoints();
app.MapQaEndpoints();
app.MapAssignmentEndpoints();
app.MapStudentEndpoints();

app.Run();

public partial class Program { }
