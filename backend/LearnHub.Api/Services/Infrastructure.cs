using System.Globalization;
using LearnHub.Api.Data;
using LearnHub.Api.Models;

namespace LearnHub.Api.Services;

public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>Real time. Set LEARNHUB_NOW (for example 2026-10-01T12:00:00Z) to pin the clock.</summary>
public class SystemClock : IClock
{
    private readonly DateTime? _fixed;

    public SystemClock(IConfiguration config)
    {
        var text = config["LEARNHUB_NOW"];
        if (!string.IsNullOrWhiteSpace(text) &&
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value))
            _fixed = value;
    }

    public DateTime UtcNow => _fixed ?? DateTime.UtcNow;
}

/// <summary>Maps the studentName used by the API to a row in the students table.</summary>
public class StudentDirectory
{
    private readonly LearnHubDbContext _db;
    private readonly IClock _clock;

    public StudentDirectory(LearnHubDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public Student? Find(string? name)
    {
        var text = name?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        var lower = text.ToLower();
        return _db.Students.FirstOrDefault(s => s.FullName.ToLower() == lower);
    }

    public Student GetOrCreate(string name)
    {
        var existing = Find(name);
        if (existing is not null) return existing;

        var text = name.Trim();
        var slug = string.Join(".", text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var email = $"{slug}@students.learnhub.test";
        var n = 1;
        while (_db.Students.Any(s => s.Email == email)) email = $"{slug}{++n}@students.learnhub.test";

        var student = new Student { FullName = text, Email = email, JoinedAt = _clock.UtcNow };
        _db.Students.Add(student);
        _db.SaveChanges();
        return student;
    }
}

public static class Money
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    public static string Fmt(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
