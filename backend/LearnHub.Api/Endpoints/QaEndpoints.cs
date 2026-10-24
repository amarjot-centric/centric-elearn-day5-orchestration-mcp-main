using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record QuestionRequest(string? StudentName, string? Body);
public record AnswerRequest(string? StudentName, int? InstructorId, string? Body);
public record AcceptRequest(int InstructorId);

public static class QaEndpoints
{
    public static void MapQaEndpoints(this WebApplication app)
    {
        app.MapGet("/api/courses/{id:int}/questions", (int id, QaService qa, CourseService courses) =>
            courses.Get(id) is null ? Results.NotFound() : Results.Ok(qa.GetQuestions(id)));

        app.MapPost("/api/courses/{id:int}/questions", (int id, QuestionRequest request, QaService qa) =>
        {
            var (question, error) = qa.Ask(id, request.StudentName, request.Body);
            if (question is null)
                return error == "Course not found." ? Results.NotFound() : Results.BadRequest(new { error });
            return Results.Created($"/api/courses/{id}/questions", question);
        });

        app.MapPost("/api/questions/{id:int}/answers", (int id, AnswerRequest request, QaService qa) =>
        {
            var (answer, error) = qa.Answer(id, request.StudentName, request.InstructorId, request.Body);
            if (answer is null)
                return error == "Question not found." ? Results.NotFound() : Results.BadRequest(new { error });
            return Results.Created($"/api/questions/{id}/answers", answer);
        });

        app.MapPost("/api/answers/{id:int}/accept", (int id, AcceptRequest request, QaService qa) =>
        {
            var (answer, error) = qa.Accept(id, request.InstructorId);
            if (answer is null)
                return error switch
                {
                    "Answer not found." => Results.NotFound(),
                    _ => Results.Json(new { error }, statusCode: StatusCodes.Status403Forbidden)
                };
            return Results.Ok(answer);
        });
    }
}
