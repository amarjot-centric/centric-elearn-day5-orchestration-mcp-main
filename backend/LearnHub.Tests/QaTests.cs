namespace LearnHub.Tests;

public class QaTests
{
    [Fact]
    public void Question_AndAnswers_AreListedTogether()
    {
        using var env = new TestEnv();
        var q = env.Qa.Ask(1, "Asha", "How do I run the app?").Question!;
        Assert.NotNull(env.Qa.Answer(q.Id, "Ben", null, "Use dotnet run").Answer);
        Assert.NotNull(env.Qa.Answer(q.Id, null, 1, "Run it from the project folder").Answer);

        var item = Assert.Single(env.Qa.GetQuestions(1));
        Assert.Equal("Asha", item.StudentName);
        Assert.Equal(2, item.Answers.Count);
        Assert.Contains(item.Answers, a => a.AuthorRole == "Instructor" && a.AuthorName == "Maya Rao");
        Assert.False(item.HasAcceptedAnswer);
    }

    [Fact]
    public void Question_NeedsBodyNameAndCourse()
    {
        using var env = new TestEnv();
        Assert.Equal("body is required.", env.Qa.Ask(1, "Asha", " ").Error);
        Assert.Equal("studentName is required.", env.Qa.Ask(1, "", "Hi?").Error);
        Assert.Equal("Course not found.", env.Qa.Ask(99, "Asha", "Hi?").Error);
    }

    [Fact]
    public void OnlyTheCourseInstructor_CanAcceptAnAnswer_AndOnlyOneIsAccepted()
    {
        using var env = new TestEnv();
        var q = env.Qa.Ask(1, "Asha", "Why?").Question!;
        var a1 = env.Qa.Answer(q.Id, "Ben", null, "Because").Answer!;
        var a2 = env.Qa.Answer(q.Id, null, 1, "Because of this").Answer!;

        Assert.Equal("Only the instructor of this course can accept an answer.", env.Qa.Accept(a1.Id, 2).Error);
        Assert.NotNull(env.Qa.Accept(a1.Id, 1).Answer);
        Assert.NotNull(env.Qa.Accept(a2.Id, 1).Answer);

        var item = Assert.Single(env.Qa.GetQuestions(1));
        Assert.True(item.HasAcceptedAnswer);
        Assert.Equal(1, item.Answers.Count(a => a.IsAccepted));
        Assert.Equal(a2.Id, item.Answers[0].Id);
    }
}
