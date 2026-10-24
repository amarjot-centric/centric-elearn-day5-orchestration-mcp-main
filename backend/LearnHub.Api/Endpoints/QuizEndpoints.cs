using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record QuizAnswer(int QuestionId, int SelectedIndex);
public record QuizSubmitRequest(string? StudentName, List<QuizAnswer>? Answers);

public static class QuizEndpoints
{
    public static void MapQuizEndpoints(this WebApplication app)
    {
        app.MapGet("/api/lessons/{lessonId:int}/quiz", (int lessonId, QuizService quizzes) =>
        {
            var questions = quizzes.GetQuiz(lessonId);
            if (questions.Count == 0) return Results.NotFound();

            // The correct answer is never sent to the browser.
            return Results.Ok(questions.Select(q => new { q.Id, q.LessonId, q.Question, q.Options }));
        });

        app.MapPost("/api/lessons/{lessonId:int}/quiz/submit", (int lessonId, QuizSubmitRequest request, QuizService quizzes) =>
        {
            if (string.IsNullOrWhiteSpace(request.StudentName))
                return Results.BadRequest(new { error = "studentName is required." });
            if (request.Answers is null || request.Answers.Count == 0)
                return Results.BadRequest(new { error = "answers are required." });
            if (quizzes.GetQuiz(lessonId).Count == 0) return Results.NotFound();

            var result = quizzes.ScoreQuiz(lessonId, request.Answers.Select(a => (a.QuestionId, a.SelectedIndex)));
            return Results.Ok(new { score = result.Score, total = result.Total, passed = result.Passed });
        });
    }
}
