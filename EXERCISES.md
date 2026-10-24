# LearnHub - Day 5 Exercises

Use Claude Code for everything in these exercises. The database is LearnHub on MySQL (see the README for how to load it).
Treat today as 1 October 2026.

---

## Exercise 1 — Refund audit

Finance has asked you to check whether refunds have been paid out against the rules. The policy is: a refund is allowed only
within 7 days of purchase and only when the student's progress in the course is below 30%. The finance director wants to know what it cost
and why it was possible.

**Requirements**

1. Find every refund that broke the policy.
2. For each one, report the order id, the student, the course or courses, the amount refunded and the coupon used (if any).
3. Report the total money refunded against policy, and split it by month of refund and by coupon.
4. Explain how the system allowed these refunds to happen.
5. Fix the problem so it cannot happen again.
6. Add tests that prove the fix.

**Deliver:** `reports/refund-audit.md`, the code fix, and the tests.

---

## Exercise 2 — Coupon abuse

Marketing believes some discount coupons were used more than they should have been. Each coupon has a usage limit, and each
student is meant to use a given coupon only once. They want to know how much was given away and why the system let it happen.

**Requirements**

1. List the coupons that were used more times than their limit allows. A refunded order does not count as a use of the coupon.
2. List the students who used the same coupon more than once (count paid orders only).
3. Work out how much money was given away beyond the limits, per coupon. Uses beyond the limit are the latest ones.
4. Find the cause in the code.
5. Fix it so that both rules are always enforced.
6. Add tests that prove the fix.

**Deliver:** `reports/coupon-audit.md`, the code fix, and the tests.

---

## Exercise 3 — Instructor payout reconciliation

Finance pays each instructor 70% of their net sales, meaning sales after refunds. The month-end payouts report does not seem to agree with
the instructor revenue figures, and finance needs to know who was overpaid or underpaid.

**Requirements**

1. Calculate what each instructor should have been paid, per month.
2. Compare it with what the payouts report shows, per instructor and per month.
3. Report the total amount over-paid or under-paid.
4. Explain the cause of the gap.
5. Fix the payouts report so it matches the correct figures.
6. Add tests that prove the fix.

**Deliver:** `reports/payouts.md`, the code fix, and the tests.

---

## Exercise 4 — Learner drop-off and at-risk students

The Head of Learning wants to understand where students give up, and wants the support team to reach out to students who are slipping
before they leave for good. This week's call list is needed by the end of the day.

**Requirements**

1. Identify the 5 courses with the worst completion rate, across all courses free and paid. Completion rate is the number of enrollments where every lesson is complete divided by the number of enrollments.
2. For each of those courses, identify the lesson where most students stop.
3. Produce a list of students to contact this week: students who are enrolled and have made no progress in the last 14 days (no lesson completed in that time), or who have an assignment overdue (past its due date with no submission).
4. For every student on the list, include a one-line suggested message that fits their situation.
5. Explain in the report how you decided who belongs on the list.

**Deliver:** `reports/learner-insights.md` and `reports/at-risk-students.csv`.

---

## Exercise 5 — Review integrity and prerequisites

Trust and Safety has received complaints that some course ratings look inflated. Separately, the Head of Learning has noticed students
turning up in advanced courses without the foundation they are meant to have.

**Requirements**

1. Find suspicious reviews: reviews written by students who are not enrolled in the course, and unusual bursts of 5-star reviews on one course in one day from newly created accounts.
2. For each affected course, show the average rating before and after removing the suspicious reviews.
3. Explain how the system allowed these reviews.
4. Fix it so only legitimate reviews can be written.
5. Add tests that prove the review fix.
6. Check whether any students are enrolled in a course whose prerequisite course they had not finished (all its lessons) when they enrolled, and explain how that happened.
7. Fix the prerequisite problem so it cannot happen again.
8. Add tests that prove the prerequisite fix.

**Deliver:** `reports/reviews.md`, the code fixes, and the tests.

---

## Exercise 6 — New feature: Live Classes with a waitlist

Instructors want to run live sessions alongside their recorded courses, and students want to book a seat. Popular sessions will fill up, so
nobody should be turned away without a place in the queue.

**Requirements**

1. Instructors can schedule a live session for one of their courses, with a start time, duration, capacity and join link.
2. Students can register for a session.
3. A student cannot be registered for two sessions that overlap in time.
4. When a session is full, a student who tries to register joins the waitlist instead.
5. When someone cancels, the first person on the waitlist is moved in automatically.
6. An instructor can see the attendees and the waitlist for each of their sessions.
7. Add the new tables to `db/schema.sql` and seed them with at least 20 sessions, including some that are full and have a waitlist.
8. Add the endpoints, the pages for students and instructors, and tests.
9. Using your seeded data, report which time slots filled up fastest.

**Deliver:** the feature (schema, seed data, endpoints, pages), tests, and `reports/live-classes.md`.
