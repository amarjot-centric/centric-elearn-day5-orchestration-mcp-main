using LearnHub.Api.Services;

namespace LearnHub.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapGet("/api/orders", (string? studentName, ShopService shop) => shop.GetOrders(studentName));

        app.MapPost("/api/orders/{id:int}/refund", (int id, ShopService shop) =>
        {
            var (order, error) = shop.RefundOrder(id);
            if (order is null)
                return error switch
                {
                    "Order not found." => Results.NotFound(),
                    "This order was already refunded." => Results.Conflict(new { error }),
                    _ => Results.BadRequest(new { error })
                };

            return Results.Ok(order);
        });
    }
}
