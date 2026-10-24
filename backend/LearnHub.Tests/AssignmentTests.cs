namespace LearnHub.Tests;

public class AssignmentTests
{
    private static (TestEnv Env, int AssignmentId) Setup()
    {
        var env = new TestEnv();
        var a = env.Assignments.Create(1, "Hello program", "Write a hello world", TestData.Today.AddDays(5)).Assignment!;
        env.Enrollments.Enroll(1, "Asha");
        return (env, a.Id);
    }

    [Fact]
    public void Assignment_NeedsTitleDueDateAndCourse()
    {
        using var env = new TestEnv();
        Assert.Equal("title is required.", env.Assignments.Create(1, " ", "", TestData.Today).Error);
        Assert.Equal("dueAt is required.", env.Assignments.Create(1, "T", "", null).Error);
        Assert.Equal("Course not found.", env.Assignments.Create(99, "T", "", TestData.Today).Error);
    }

    [Fact]
    public void Submit_RequiresEnrollment()
    {
        var (env, id) = Setup();
        using (env)
            Assert.Equal("Student is not enrolled in this course.", env.Assignments.Submit(id, "Ben", "my work").Error);
    }

    [Fact]
    public void Submit_ThenResubmit_ReplacesUntilGraded()
    {
        var (env, id) = Setup();
        using (env)
        {
            var first = env.Assignments.Submit(id, "Asha", "v1").Submission!;
            var second = env.Assignments.Submit(id, "asha", "v2").Submission!;
            Assert.Equal(first.Id, second.Id);
            Assert.Equal("v2", env.Assignments.GetSubmissions(id).Single().Body);

            env.Assignments.Grade(first.Id, 80, "Nice");
            Assert.Equal("This submission was already graded.", env.Assignments.Submit(id, "Asha", "v3").Error);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Grade_MustBeZeroToHundred(int grade)
    {
        var (env, id) = Setup();
        using (env)
        {
            var s = env.Assignments.Submit(id, "Asha", "work").Submission!;
            Assert.Equal("grade must be between 0 and 100.", env.Assignments.Grade(s.Id, grade, "x").Error);
        }
    }

    [Fact]
    public void Grade_SavesGradeAndFeedback_AndStudentSeesIt()
    {
        var (env, id) = Setup();
        using (env)
        {
            var s = env.Assignments.Submit(id, "Asha", "work").Submission!;
            var graded = env.Assignments.Grade(s.Id, 92, "Well done").Submission!;
            Assert.Equal(92, graded.Grade);
            Assert.NotNull(graded.GradedAt);

            var mine = Assert.Single(env.Assignments.ForStudent("Asha"));
            Assert.True(mine.Submitted);
            Assert.Equal(92, mine.Grade);
            Assert.Equal("Well done", mine.Feedback);
            Assert.Equal(1, env.Assignments.ForInstructor(1).Single().SubmissionCount);
        }
    }
}
