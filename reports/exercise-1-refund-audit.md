# Exercise 1 — Refund audit

Scope: all 150 rows in `refunds` (live `learnhub` DB), checked against the policy: refund only within 7 days of purchase **and** progress below 30% on every course in the order. "Today" is 2026-10-01.

## Result

**9 of 150 refunds broke policy, totalling 544.16.**

| Order | Student | Course(s) | Refunded | Coupon | Refund month | Breach |
|---|---|---|---|---|---|---|
| 247 | Sara Ali | TypeScript in Depth; Building REST APIs with .NET | 128.00 | – | 2025-12 | Progress 5/6 (83%) and 4/6 (67%) |
| 290 | Rhea Kowalski | React Fundamentals | 49.00 | – | 2025-12 | Refunded 22.7 days after purchase |
| 435 | Kavya Fernandez | Business Writing | 19.00 | – | 2026-01 | Progress 6/7 (86%) |
| 838 | Victor Abdi | Serverless Applications | 35.40 | FLASH40 | 2026-05 | Progress 4/6 (67%) |
| 762 | Quinn Reyes | Design Systems | 41.40 | FLASH40 | 2026-06 | 28.5 days |
| 778 | Yara Cohen | Python for Data Analysis; Motion and Interaction Design | 64.80 | FLASH40 | 2026-06 | 24.8 days |
| 867 | Wen Tanaka | Object-Oriented Design in C#; Project Management Basics; React Fundamentals; Finance for Non-Finance Managers | 110.16 | FLASH40 | 2026-06 | 11.3 days |
| 908 | Rhea Yilmaz | Cloud Networking; Data Analysis with SQL | 47.40 | FLASH40 | 2026-06 | 17.2 days |
| 1025 | Olga Garcia | Cloud Networking | 49.00 | – | 2026-08 | 26.3 days |

### By month of refund
| Month | Amount |
|---|---|
| 2025-12 | 177.00 |
| 2026-01 | 19.00 |
| 2026-05 | 35.40 |
| 2026-06 | 263.76 |
| 2026-08 | 49.00 |
| **Total** | **544.16** |

### By coupon
| Coupon | Orders | Amount |
|---|---|---|
| none | 4 (247, 290, 435, 1025) | 245.00 |
| FLASH40 | 5 (762, 778, 838, 867, 908) | 299.16 |
| **Total** | 9 | **544.16** |

### By breach type
- Past 7 days: 6 orders (290, 762, 778, 867, 908, 1025), 361.76.
- Progress of 30% or more: 3 orders (247, 435, 838), 182.40.

FLASH40 accounts for 55% of the money, and four of its five orders were refunded in June 2026. That pattern looks like coupon abuse (buy cheap, refund late); it belongs in the coupon exercise and is not addressed here.

### Limit of this audit
`RefundOrder` deletes the student's `lesson_progress` rows when it refunds. Progress at refund time can't be recovered for the other 141 refunds, so **the progress count is a lower bound**. The three progress breaches are visible only because their rows survived (all completed before the refund date). The time check uses `orders.created_at` and `refunds.refunded_at` and is exact.

## How the system allowed it

1. **Wrong date in the window check.** `ShopService.RefundOrder` compared `now` to `order.UpdatedAt`, not `CreatedAt`. `UpdatedAt` moves whenever an order is touched, and 94 orders that are still `Paid` already have `updated_at` different from `created_at` (66 of them by more than 7 days). Any such touch restarted the 7-day window.
2. **Nothing outside the API enforces the policy.** The three progress breaches kept their lesson rows, which the API always deletes on refund. They were therefore refunded outside `RefundOrder`, with a direct database write. The `refunds` table has only a unique `order_id` and a foreign key; MySQL can't express "age and progress" as a constraint.
3. **No evidence kept.** A refund stores only the amount. Age and progress at refund time aren't recorded, and the progress rows are deleted, so breaches can't be audited afterwards.

## Fix

- [ShopService.cs](../backend/LearnHub.Api/Services/ShopService.cs): the 7-day window is now measured from `order.CreatedAt`. The progress check is unchanged. Exactly 7 days is still allowed.
- No schema change and no change to existing data. The 9 refunds above are not reversed.

**Recommendations (not done):**
- Record progress and days-since-purchase on the `refunds` row, so the audit survives the progress deletion.
- Route every refund through the API, and restrict direct write access to the `orders` and `refunds` tables.
- Decide with finance whether the 544.16 is to be recovered.

## Tests

In [ShopTests.cs](../backend/LearnHub.Tests/ShopTests.cs):
- `R18_Refund_WindowRunsFromPurchase_EvenIfOrderUpdatedAtMoved`: `UpdatedAt` is moved to day 6, the refund is attempted on day 8 and refused. **Fails on the old code, passes now.**
- `R18_Refund_IsRefused_WhenOneCourseOfSeveralIsOverTheProgressLimit`: a two-course order with one course at 40% is refused. The order stays Paid, there is no refund row, and the enrollments are kept.
- `R18_Refund_IsAllowed_WhenProgressIsUnderTheLimit`: 20% progress is refunded.
- The existing tests still cover the 7-day boundary and the 40% refusal.

All 26 `ShopTests` pass.

## Reproducing the audit (read-only SQL)

```sql
-- late refunds
SELECT o.id, r.amount, o.coupon_code, o.created_at, r.refunded_at
FROM refunds r JOIN orders o ON o.id = r.order_id
WHERE TIMESTAMPDIFF(SECOND, o.created_at, r.refunded_at) > 7*86400;

-- surviving progress >= 30% on a refunded order's courses
SELECT o.id, l.course_id, pc.done,
       (SELECT COUNT(*) FROM lessons x WHERE x.course_id = l.course_id) AS lessons
FROM orders o
JOIN refunds r ON r.order_id = o.id
JOIN order_lines l ON l.order_id = o.id
JOIN (SELECT student_id, course_id, COUNT(*) done FROM lesson_progress GROUP BY 1,2) pc
  ON pc.student_id = o.student_id AND pc.course_id = l.course_id
WHERE 100.0 * pc.done / (SELECT COUNT(*) FROM lessons x WHERE x.course_id = l.course_id) >= 30;
```
