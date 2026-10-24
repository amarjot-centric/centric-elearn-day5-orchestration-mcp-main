using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class QuizService
{
    private readonly LearnHubDbContext _db;

    public QuizService(LearnHubDbContext db) => _db = db;

    public List<QuizQuestion> GetQuiz(int lessonId) =>
        _db.QuizQuestions.AsNoTracking().Where(q => q.LessonId == lessonId).OrderBy(q => q.Id).ToList();

    public QuizScore ScoreQuiz(int lessonId, IEnumerable<(int QuestionId, int SelectedIndex)> answers)
    {
        var given = answers.ToList();
        var questions = GetQuiz(lessonId);
        var score = questions.Count(q => given.Any(a => a.QuestionId == q.Id && a.SelectedIndex == q.CorrectIndex));
        var total = questions.Count;
        return new QuizScore(score, total, total > 0 && score * 2 >= total);
    }
}
