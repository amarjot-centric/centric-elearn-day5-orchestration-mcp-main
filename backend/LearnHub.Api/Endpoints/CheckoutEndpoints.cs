using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public record CheckoutRequest(string? StudentName, string? CouponCode);

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this WebApplication app)
    {
        app.MapPost("/api/checkout/preview", (CheckoutRequest request, ShopService shop) =>
        {
            var quote = shop.Quote(request.StudentName, request.CouponCode);
            return Results.Ok(new
            {
                Lines = quote.Lines.Select(l => new { l.CourseId, l.Title, l.Price, l.BundleDiscount, l.CouponDiscount, l.Final }),
                quote.Subtotal,
                quote.BundleDiscount,
                quote.CouponDiscount,
                quote.Total,
                quote.CouponMessage
            });
        });

        app.MapPost("/api/checkout", (CheckoutRequest request, ShopService shop) =>
        {
            var (order, error) = shop.Checkout(request.StudentName, request.CouponCode);
            if (order is null) return Results.BadRequest(new { error });

            return Results.Created($"/api/orders/{order.Id}", order);
        });
    }
}
