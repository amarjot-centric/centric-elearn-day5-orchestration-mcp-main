using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record CartRequest(string? StudentName, int CourseId);

public static class CartEndpoints
{
    public static void MapCartEndpoints(this WebApplication app)
    {
        app.MapGet("/api/cart", (string? studentName, ShopService shop) => shop.GetCart(studentName));

        app.MapPost("/api/cart", (CartRequest request, ShopService shop) =>
        {
            var (item, error) = shop.AddToCart(request.StudentName, request.CourseId);
            if (item is null)
                return error is "You already own this course." or "Already in your cart."
                    ? Results.Conflict(new { error })
                    : Results.BadRequest(new { error });

            return Results.Created($"/api/cart?studentName={Uri.EscapeDataString(item.StudentName)}", item);
        });

        app.MapDelete("/api/cart", (string? studentName, int courseId, ShopService shop) =>
            shop.RemoveFromCart(studentName, courseId) ? Results.NoContent() : Results.NotFound());
    }
}
