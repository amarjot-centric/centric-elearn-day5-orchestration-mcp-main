using System.Text.Json;
using LearnHub.Api.Data;
using LearnHub.Api.Models;

namespace LearnHub.Tests;

/// <summary>A small, fixed data set: 6 courses, 24 lessons, 22 quiz questions and 5 coupons.</summary>
public static class TestData
{
    public static readonly DateTime Today = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public static void Seed(LearnHubDbContext db)
    {
        var joined = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        string[] instructors = { "Maya Rao", "Liam Carter", "Sofia Alvarez", "Ken Watanabe", "Priya Nair", "Omar Hassan" };
        for (var i = 0; i < instructors.Length; i++)
            db.Instructors.Add(new Instructor
            {
                Id = i + 1, FullName = instructors[i], Bio = "Teaches on LearnHub.", JoinedAt = joined,
                Email = instructors[i].ToLowerInvariant().Replace(' ', '.') + "@learnhub.test"
            });

        var lessonId = 1;
        void AddCourse(int id, int instructorId, string title, string description, string category, string level, decimal price,
            params (string Title, int Minutes, string Content)[] lessons)
        {
            var course = new Course
            {
                Id = id, InstructorId = instructorId, Title = title, ShortDescription = description, Category = category,
                Level = level, Price = price, CreatedAt = joined, UpdatedAt = joined
            };
            var position = 1;
            foreach (var l in lessons)
                course.Lessons.Add(new Lesson { Id = lessonId++, Position = position++, Title = l.Title, DurationMinutes = l.Minutes, Content = l.Content });
            db.Courses.Add(course);
        }

        AddCourse(1, 1, "Intro to C# Programming", "Learn the basics of C# by writing small programs.", "Programming", "Beginner", 0m,
            ("Setting up your tools", 10, "Install the .NET SDK and run your first program."),
            ("Variables and types", 15, "Store numbers, text and true/false values."),
            ("Conditions and loops", 20, "Make decisions and repeat work with if and for."),
            ("Methods", 15, "Group code into small reusable pieces."));
        AddCourse(2, 2, "JavaScript for the Web", "Build interactive web pages with modern JavaScript.", "Programming", "Intermediate", 40m,
            ("The DOM", 15, "Find and change things on a web page."),
            ("Events", 12, "React to clicks and key presses."),
            ("Fetching data", 18, "Call an API with fetch and show the result."),
            ("Async code", 20, "Understand promises and async/await."));
        AddCourse(3, 3, "UI Design Basics", "Learn layout, colour and type for clean interfaces.", "Design", "Beginner", 30m,
            ("Layout and spacing", 12, "Use a grid and consistent spacing."),
            ("Colour", 10, "Pick an accent colour and keep contrast high."),
            ("Typography", 12, "Choose readable fonts and sizes."));
        AddCourse(4, 4, "Data Analysis with SQL", "Ask questions of data using SQL queries.", "Data", "Intermediate", 30m,
            ("SELECT basics", 15, "Read rows from a table."),
            ("Filtering and sorting", 15, "Use WHERE and ORDER BY."),
            ("Joins", 20, "Combine data from two tables."),
            ("Grouping", 18, "Count and total with GROUP BY."),
            ("Query practice", 25, "Solve small real-world questions."));
        AddCourse(5, 5, "Cloud Fundamentals", "Understand servers, storage and networks in the cloud.", "Cloud", "Beginner", 0m,
            ("What is the cloud?", 10, "Compute, storage and networking in plain words."),
            ("Virtual machines", 15, "Start a server and connect to it."),
            ("Storage", 12, "Files, blocks and databases."),
            ("Cost basics", 10, "Understand what you pay for."));
        AddCourse(6, 6, "Deploying Apps with Containers", "Package an app in a container and run it anywhere.", "Cloud", "Advanced", 60m,
            ("Why containers?", 10, "The problem containers solve."),
            ("Writing a Dockerfile", 20, "Describe how to build your app image."),
            ("Running containers", 15, "Start, stop and inspect containers."),
            ("Compose", 20, "Run several containers together."));

        void Q(int id, int lesson, string question, int correct, params string[] options) =>
            db.QuizQuestions.Add(new QuizQuestion
            {
                Id = id, LessonId = lesson, Question = question, CorrectIndex = correct, OptionsJson = JsonSerializer.Serialize(options)
            });
        Q(1, 1, "Which tool do you install to run C# programs?", 0, "The .NET SDK", "Photoshop", "A web browser", "Excel");
        Q(2, 1, "What does 'dotnet run' do?", 1, "Deletes your project", "Builds and starts your program", "Installs Windows", "Opens a website");
        Q(3, 2, "Which type stores whole numbers?", 2, "string", "bool", "int", "char");
        Q(4, 2, "Which type stores true or false?", 0, "bool", "int", "double", "string");
        Q(5, 3, "Which keyword repeats code a set number of times?", 1, "if", "for", "class", "using");
        Q(6, 3, "What does 'if' do?", 1, "Repeats code", "Makes a decision", "Names a method", "Imports a library");
        Q(7, 4, "Why use methods?", 1, "To make code slower", "To group code into reusable pieces", "To delete variables", "To change colours");
        Q(8, 4, "Which word means a method returns nothing?", 2, "null", "empty", "void", "none");
        Q(9, 5, "What does DOM stand for?", 0, "Document Object Model", "Data Output Mode", "Digital Order Map", "Display Object Menu");
        Q(10, 5, "Which call finds an element by its id?", 0, "document.getElementById", "document.makeId", "window.find", "page.select");
        Q(11, 6, "Which event fires when a button is clicked?", 1, "hover", "click", "load", "scroll");
        Q(12, 6, "What does addEventListener do?", 0, "Runs code when an event happens", "Adds a CSS class", "Deletes an element", "Starts a server");
        Q(13, 7, "Which function calls an API from the browser?", 0, "fetch", "send", "pull", "load");
        Q(14, 7, "What format do most APIs return?", 0, "JSON", "MP3", "PDF", "ZIP");
        Q(15, 8, "What does 'await' do?", 1, "Stops the browser", "Waits for a promise to finish", "Deletes a promise", "Repeats a loop");
        Q(16, 8, "Which keyword marks a function that can use await?", 0, "async", "defer", "later", "wait");
        Q(17, 9, "What helps keep a layout tidy?", 1, "Random spacing", "A grid and consistent spacing", "Many fonts", "Tiny text");
        Q(18, 9, "What is white space good for?", 1, "Wasting room", "Making content easier to read", "Hiding content", "Slowing the page");
        Q(19, 10, "What should you keep high for text colour?", 0, "Contrast", "Noise", "Saturation only", "Opacity only");
        Q(20, 10, "How many accent colours should a simple design start with?", 0, "One", "Ten", "Twenty", "None");
        Q(21, 11, "What makes text readable?", 0, "Clear fonts and good size", "Very small text", "Many styles at once", "Light grey on white");
        Q(22, 11, "Which is a good body text size on the web?", 1, "4px", "16px", "60px", "2px");

        var forever = new DateTime(2099, 12, 31);
        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", DiscountType = "Percent", DiscountValue = 10, ExpiresOn = forever, MaxUses = 1000 },
            new Coupon { Code = "SAVE20", DiscountType = "Fixed", DiscountValue = 20, ExpiresOn = forever, MaxUses = 1000, MinOrderAmount = 50 },
            new Coupon { Code = "CLOUD25", DiscountType = "Percent", DiscountValue = 25, ExpiresOn = forever, MaxUses = 100, Category = "Cloud" },
            new Coupon { Code = "FLAT50", DiscountType = "Fixed", DiscountValue = 50, ExpiresOn = forever, MaxUses = 100 },
            new Coupon { Code = "OLD5", DiscountType = "Percent", DiscountValue = 5, ExpiresOn = new DateTime(2024, 12, 31), MaxUses = 100 });

        db.SaveChanges();
    }
}
