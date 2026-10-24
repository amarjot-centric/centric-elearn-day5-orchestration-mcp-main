using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public static class CertificateEndpoints
{
    public static void MapCertificateEndpoints(this WebApplication app)
    {
        app.MapGet("/api/certificates", (string? studentName, ProgressService progress) =>
            progress.GetCertificates(studentName));
    }
}
