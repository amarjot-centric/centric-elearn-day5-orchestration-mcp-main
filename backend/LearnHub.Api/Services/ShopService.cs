using System.Globalization;
using LearnHub.Api.Data;
using LearnHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Services;

public class QuoteLine
{
    public int CourseId { get; set; }
    public string Title { get; set; } = "";
    public string Instructor { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public decimal BundleDiscount { get; set; }
    public decimal CouponDiscount { get; set; }
    public decimal Final { get; set; }
}

/// <summary>The price breakdown of a cart. Used by the preview and by checkout.</summary>
public class PriceQuote
{
    public List<QuoteLine> Lines { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal BundleDiscount { get; set; }
    public decimal CouponDiscount { get; set; }
    public decimal Total { get; set; }
    public string CouponMessage { get; set; } = "";
    public bool CouponRejected { get; set; }
    public string? CouponCode { get; set; }
}

public class ShopService
{
    private const decimal BundleRate = 0.15m;
    private const int BundleMinCourses = 3;
    private const decimal InstructorShareRate = 0.70m;
    private const int RefundDays = 7;
    private const double RefundMaxProgressPercent = 30;

    private readonly LearnHubDbContext _db;
    private readonly StudentDirectory _students;
    private readonly EnrollmentService _enrollments;
    private readonly ProgressService _progress;
    private readonly IClock _clock;

    public ShopService(LearnHubDbContext db, StudentDirectory students, EnrollmentService enrollments,
        ProgressService progress, IClock clock)
    {
        _db = db;
        _students = students;
        _enrollments = enrollments;
        _progress = progress;
        _clock = clock;
    }

    // ---- Cart ----

    /// <summary>Returns the new cart item, or an error text.</summary>
    public (CartItemCreated? Item, string? Error) AddToCart(string? studentName, int courseId)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
        var course = _db.Courses.FirstOrDefault(c => c.Id == courseId);
        if (course is null) return (null, "Course not found.");
        if (course.Price <= 0) return (null, "This course is free. Enroll in it directly.");
        var student = _students.GetOrCreate(name);
        if (_db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == courseId)) return (null, "You already own this course.");
        if (_db.CartItems.Any(i => i.StudentId == student.Id && i.CourseId == courseId)) return (null, "Already in your cart.");

        var item = new CartItem { StudentId = student.Id, CourseId = courseId, AddedAt = _clock.UtcNow };
        _db.CartItems.Add(item);
        _db.SaveChanges();
        return (new CartItemCreated(student.FullName, courseId, item.AddedAt), null);
    }

    /// <summary>Returns true if an item was removed.</summary>
    public bool RemoveFromCart(string? studentName, int courseId)
    {
        var student = _students.Find(studentName);
        if (student is null) return false;
        var item = _db.CartItems.FirstOrDefault(i => i.StudentId == student.Id && i.CourseId == courseId);
        if (item is null) return false;
        _db.CartItems.Remove(item);
        _db.SaveChanges();
        return true;
    }

    public List<CartLineView> GetCart(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return new();
        var ids = _db.CartItems.AsNoTracking().Where(i => i.StudentId == student.Id).OrderBy(i => i.Id)
            .Select(i => i.CourseId).ToList();
        var courses = _db.Courses.AsNoTracking().Where(c => ids.Contains(c.Id))
            .Select(c => new CartLineView(c.Id, c.Title, c.Price, c.Category, c.Instructor.FullName))
            .ToDictionary(c => c.CourseId);
        return ids.Select(id => courses[id]).ToList();
    }

    // ---- Prices: bundle and coupon ----

    /// <summary>Preview of the cart price. Changes nothing.</summary>
    public PriceQuote Quote(string? studentName, string? couponCode)
    {
        var code = couponCode?.Trim();
        if (!string.IsNullOrEmpty(code) && HasUsedCoupon(studentName ?? "", code))
        {
            var quote = BuildQuote(studentName, null);
            if (quote.Lines.Count > 0)
            {
                quote.CouponRejected = true;
                quote.CouponMessage = "You have already used this coupon.";
            }
            return quote;
        }
        return BuildQuote(studentName, couponCode);
    }

    /// <summary>Works out the price of the student's cart. Bundle first, then the coupon.</summary>
    private PriceQuote BuildQuote(string? studentName, string? couponCode)
    {
        var student = _students.Find(studentName);
        var items = student is null
            ? new List<CartItem>()
            : _db.CartItems.AsNoTracking().Where(i => i.StudentId == student.Id).OrderBy(i => i.Id).ToList();
        var courseIds = items.Select(i => i.CourseId).ToList();
        var courses = _db.Courses.AsNoTracking().Include(c => c.Instructor).Where(c => courseIds.Contains(c.Id)).ToDictionary(c => c.Id);

        var quote = new PriceQuote
        {
            Lines = items.Select(i =>
            {
                var c = courses[i.CourseId];
                return new QuoteLine
                {
                    CourseId = c.Id, Title = c.Title, Instructor = c.Instructor.FullName, Category = c.Category, Price = c.Price
                };
            }).ToList()
        };
        var lines = quote.Lines;

        // 1. Bundle: 3 or more paid courses get 15% off every line.
        if (lines.Count >= BundleMinCourses)
            foreach (var line in lines) line.BundleDiscount = Money.Round(line.Price * BundleRate);

        // 2. Coupon, worked out on the prices after the bundle.
        var code = couponCode?.Trim();
        if (!string.IsNullOrEmpty(code) && lines.Count > 0)
        {
            var error = ApplyCoupon(code, quote, out var applied);
            if (error is not null)
            {
                quote.CouponRejected = true;
                quote.CouponMessage = error;
            }
            else
            {
                quote.CouponCode = applied!.Code;
                quote.CouponMessage = applied.DiscountType == "Percent"
                    ? $"Coupon {applied.Code} applied: {applied.DiscountValue.ToString("0.##", CultureInfo.InvariantCulture)}% off."
                    : $"Coupon {applied.Code} applied: {Money.Fmt(applied.DiscountValue)} off.";
            }
        }

        foreach (var line in lines)
            line.Final = Math.Max(0, line.Price - line.BundleDiscount - line.CouponDiscount);
        quote.Subtotal = lines.Sum(l => l.Price);
        quote.BundleDiscount = lines.Sum(l => l.BundleDiscount);
        quote.CouponDiscount = lines.Sum(l => l.CouponDiscount);
        quote.Total = Math.Max(0, lines.Sum(l => l.Final));
        return quote;
    }

    /// <summary>Checks the coupon and fills CouponDiscount on the lines it applies to. Returns an error text or null.</summary>
    private string? ApplyCoupon(string code, PriceQuote quote, out Coupon? applied)
    {
        applied = null;
        var lowerCode = code.ToLower();
        var coupon = _db.Coupons.FirstOrDefault(c => c.Code.ToLower() == lowerCode);
        if (coupon is null) return "Coupon not found.";
        if (coupon.ExpiresOn.Date < _clock.UtcNow.Date) return "This coupon has expired.";
        if (coupon.UsedCount > coupon.MaxUses) return "This coupon has no uses left.";

        var afterBundle = quote.Lines.Sum(l => l.Price - l.BundleDiscount);
        if (afterBundle < coupon.MinOrderAmount)
            return $"This coupon needs an order of at least {Money.Fmt(coupon.MinOrderAmount)} after the bundle discount.";

        var eligible = quote.Lines.Where(l =>
            string.IsNullOrEmpty(coupon.Category) ||
            string.Equals(l.Category, coupon.Category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (eligible.Count == 0) return $"This coupon only works for {coupon.Category} courses.";

        var eligibleTotal = eligible.Sum(l => l.Price - l.BundleDiscount);
        if (coupon.DiscountType == "Percent")
        {
            foreach (var line in eligible)
                line.CouponDiscount = Math.Min(line.Price - line.BundleDiscount, Money.Round((line.Price - line.BundleDiscount) * coupon.DiscountValue / 100m));
        }
        else if (eligibleTotal > 0)
        {
            // A fixed amount never goes above what the matching lines cost. It is shared by line price.
            var discount = Math.Min(coupon.DiscountValue, eligibleTotal);
            var given = 0m;
            for (var i = 0; i < eligible.Count; i++)
            {
                var net = eligible[i].Price - eligible[i].BundleDiscount;
                eligible[i].CouponDiscount = i == eligible.Count - 1 ? discount - given : Money.Round(discount * net / eligibleTotal);
                given += eligible[i].CouponDiscount;
            }
        }
        applied = coupon;
        return null;
    }

    public bool HasUsedCoupon(string studentName, string code)
    {
        var student = _students.Find(studentName);
        if (student is null) return false;
        var lowerCode = code.ToLower();
        return _db.Orders.Any(o => o.StudentId == student.Id && o.Status == "Paid" && o.CouponCode != null &&
            o.CouponCode.ToLower() == lowerCode);
    }

    // ---- Checkout and orders ----

    /// <summary>Re-checks the cart, then saves the order. Returns the order, or an error text.</summary>
    public (OrderView? Order, string? Error) Checkout(string? studentName, string? couponCode)
    {
        var name = studentName?.Trim();
        if (string.IsNullOrEmpty(name)) return (null, "studentName is required.");
        var student = _students.Find(name);
        var cart = student is null
            ? new List<CartItem>()
            : _db.CartItems.Where(i => i.StudentId == student.Id).ToList();
        if (student is null || cart.Count == 0) return (null, "Your cart is empty.");

        foreach (var item in cart)
            if (_db.Enrollments.Any(e => e.StudentId == student.Id && e.CourseId == item.CourseId))
                return (null, $"You already own {_db.Courses.First(c => c.Id == item.CourseId).Title}. Remove it from your cart.");

        var quote = BuildQuote(name, couponCode);
        if (quote.CouponRejected) return (null, quote.CouponMessage);

        var now = _clock.UtcNow;
        var order = new Order
        {
            StudentId = student.Id,
            CouponCode = quote.CouponCode,
            Subtotal = quote.Subtotal,
            BundleDiscount = quote.BundleDiscount,
            CouponDiscount = quote.CouponDiscount,
            Total = quote.Total,
            Status = "Paid",
            CreatedAt = now,
            UpdatedAt = now,
            Lines = quote.Lines.Select(l => new OrderLine
            {
                CourseId = l.CourseId, Price = l.Price, BundleDiscount = l.BundleDiscount,
                CouponDiscount = l.CouponDiscount, FinalPrice = l.Final
            }).ToList()
        };
        _db.Orders.Add(order);

        if (quote.CouponCode is not null)
        {
            var lowerCode = quote.CouponCode.ToLower();
            _db.Coupons.First(c => c.Code.ToLower() == lowerCode).UsedCount++;
        }
        _db.CartItems.RemoveRange(cart);
        _db.SaveChanges();

        foreach (var line in order.Lines) _enrollments.Enroll(line.CourseId, student.FullName);

        return (ToView(order.Id), null);
    }

    public List<OrderView> GetOrders(string? studentName)
    {
        var student = _students.Find(studentName);
        if (student is null) return new();
        var ids = _db.Orders.AsNoTracking().Where(o => o.StudentId == student.Id).OrderByDescending(o => o.Id)
            .Select(o => o.Id).ToList();
        return ids.Select(ToView).ToList();
    }

    private OrderView ToView(int orderId)
    {
        var o = _db.Orders.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Lines).ThenInclude(l => l.Course).ThenInclude(c => c.Instructor)
            .First(x => x.Id == orderId);
        return new OrderView(o.Id, o.Student.FullName, o.CreatedAt,
            o.Lines.OrderBy(l => l.Id).Select(l => new OrderLineView(l.CourseId, l.Course.Title, l.Course.Instructor.FullName,
                l.Price, l.BundleDiscount, l.CouponDiscount, l.FinalPrice)).ToList(),
            o.CouponCode, o.Subtotal, o.BundleDiscount, o.CouponDiscount, o.Total, o.Status);
    }

    // ---- Refunds ----

    /// <summary>Refunds an order. Returns the order, or an error text ("Order not found.", "This order was already refunded.", or the reason).</summary>
    public (OrderView? Order, string? Error) RefundOrder(int orderId)
    {
        var order = _db.Orders.Include(o => o.Lines).ThenInclude(l => l.Course).FirstOrDefault(o => o.Id == orderId);
        if (order is null) return (null, "Order not found.");
        if (order.Status == "Refunded") return (null, "This order was already refunded.");
        var now = _clock.UtcNow;
        // The window runs from purchase; UpdatedAt moves whenever the order is touched and must not reset it.
        if (now - order.CreatedAt > TimeSpan.FromDays(RefundDays))
            return (null, $"Refunds are only possible within {RefundDays} days of the order.");

        foreach (var line in order.Lines)
        {
            var percent = _progress.PercentFor(order.StudentId, line.CourseId);
            if (percent >= RefundMaxProgressPercent)
                return (null, $"You have finished {Math.Round(percent)}% of {line.Course.Title}. Refunds need progress under {RefundMaxProgressPercent}% on every course.");
        }

        order.Status = "Refunded";
        order.UpdatedAt = now;
        _db.Refunds.Add(new Refund { OrderId = order.Id, RefundedAt = now, Amount = order.Total, Reason = "Requested by the student" });
        foreach (var line in order.Lines)
        {
            _db.Enrollments.RemoveRange(_db.Enrollments.Where(e => e.CourseId == line.CourseId && e.StudentId == order.StudentId));
            _db.LessonProgress.RemoveRange(_db.LessonProgress.Where(p => p.CourseId == line.CourseId && p.StudentId == order.StudentId));
        }
        if (order.CouponCode is not null)
        {
            var lowerCode = order.CouponCode.ToLower();
            var coupon = _db.Coupons.FirstOrDefault(c => c.Code.ToLower() == lowerCode);
            if (coupon is not null && coupon.UsedCount > 0) coupon.UsedCount--;
        }
        _db.SaveChanges();
        return (ToView(order.Id), null);
    }

    // ---- Instructor revenue ----

    public List<InstructorRevenueRow> GetInstructorRevenue()
    {
        var names = _db.Courses.Select(c => c.Instructor.FullName).Distinct().ToList().OrderBy(n => n).ToList();
        var lines = _db.OrderLines.AsNoTracking()
            .Select(l => new { Instructor = l.Course.Instructor.FullName, l.FinalPrice, l.Order.Status })
            .ToList();
        return names.Select(name =>
        {
            var mine = lines.Where(l => string.Equals(l.Instructor, name, StringComparison.OrdinalIgnoreCase)).ToList();
            var gross = mine.Sum(l => l.FinalPrice);
            var refunds = mine.Where(l => l.Status == "Refunded").Sum(l => l.FinalPrice);
            return new InstructorRevenueRow(name, gross, refunds, Money.Round((gross - refunds) * InstructorShareRate));
        }).ToList();
    }
}
