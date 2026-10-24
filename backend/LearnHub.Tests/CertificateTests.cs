namespace LearnHub.Tests;

public class CertificateTests
{
    private static void CompleteAll(TestEnv env, string name, int courseId) =>
        env.CompleteLessons(name, courseId, 100);

    [Fact]
    public void NoCertificate_WhenCourseIsNotFinished()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha");
        env.Progress.MarkLessonComplete("Asha", 1, 1);

        Assert.Empty(env.Progress.GetCertificates("Asha"));
    }

    [Fact]
    public void Certificate_IsIssued_WhenAllLessonsAreComplete()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha");
        CompleteAll(env, "Asha", 1);

        var cert = Assert.Single(env.Progress.GetCertificates("asha"));

        Assert.Equal("Intro to C# Programming", cert.CourseTitle);
        Assert.Equal("Asha", cert.StudentName);
        Assert.False(string.IsNullOrWhiteSpace(cert.CertificateId));
        Assert.True(cert.IssuedOn <= DateTime.UtcNow);
    }

    [Fact]
    public void Certificates_BelongToTheRightStudent()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha");
        env.Enrollments.Enroll(1, "Ben");
        CompleteAll(env, "Asha", 1);

        Assert.Single(env.Progress.GetCertificates("Asha"));
        Assert.Empty(env.Progress.GetCertificates("Ben"));
        Assert.Empty(env.Progress.GetCertificates(""));
    }

    [Fact]
    public void CertificateIds_AreUniquePerCourse_AndStable()
    {
        using var env = new TestEnv();
        env.Enrollments.Enroll(1, "Asha");
        env.Enrollments.Enroll(2, "Asha");
        CompleteAll(env, "Asha", 1);
        CompleteAll(env, "Asha", 2);

        var first = env.Progress.GetCertificates("Asha");
        var second = env.Progress.GetCertificates("Asha");

        Assert.Equal(2, first.Select(c => c.CertificateId).Distinct().Count());
        Assert.Equal(first.Select(c => c.CertificateId), second.Select(c => c.CertificateId));
    }
}
