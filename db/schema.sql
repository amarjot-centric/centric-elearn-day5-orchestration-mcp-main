-- LearnHub schema (MySQL 8). Loading this file drops and re-creates the learnhub database.

DROP DATABASE IF EXISTS learnhub;
CREATE DATABASE learnhub CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE learnhub;

CREATE TABLE instructors (
  id          INT NOT NULL AUTO_INCREMENT,
  full_name   VARCHAR(120) NOT NULL,
  email       VARCHAR(190) NOT NULL,
  bio         VARCHAR(500) NOT NULL DEFAULT '',
  joined_at   DATETIME NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_instructors_email (email)
) ENGINE=InnoDB;

CREATE TABLE students (
  id          INT NOT NULL AUTO_INCREMENT,
  full_name   VARCHAR(120) NOT NULL,
  email       VARCHAR(190) NOT NULL,
  joined_at   DATETIME NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_students_email (email),
  KEY ix_students_full_name (full_name)
) ENGINE=InnoDB;

CREATE TABLE courses (
  id                INT NOT NULL AUTO_INCREMENT,
  instructor_id     INT NOT NULL,
  title             VARCHAR(200) NOT NULL,
  short_description VARCHAR(500) NOT NULL,
  category          VARCHAR(60) NOT NULL,
  level             VARCHAR(20) NOT NULL,
  price             DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  created_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_courses_title (title),
  KEY ix_courses_instructor (instructor_id),
  KEY ix_courses_category (category),
  CONSTRAINT fk_courses_instructor FOREIGN KEY (instructor_id) REFERENCES instructors (id),
  CONSTRAINT ck_courses_price CHECK (price >= 0)
) ENGINE=InnoDB;

CREATE TABLE course_prerequisites (
  course_id          INT NOT NULL,
  requires_course_id INT NOT NULL,
  PRIMARY KEY (course_id, requires_course_id),
  CONSTRAINT fk_prereq_course FOREIGN KEY (course_id) REFERENCES courses (id),
  CONSTRAINT fk_prereq_requires FOREIGN KEY (requires_course_id) REFERENCES courses (id)
) ENGINE=InnoDB;

CREATE TABLE lessons (
  id               INT NOT NULL AUTO_INCREMENT,
  course_id        INT NOT NULL,
  position         INT NOT NULL,
  title            VARCHAR(200) NOT NULL,
  duration_minutes INT NOT NULL,
  content          TEXT NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_lessons_position (course_id, position),
  CONSTRAINT fk_lessons_course FOREIGN KEY (course_id) REFERENCES courses (id)
) ENGINE=InnoDB;

CREATE TABLE quiz_questions (
  id            INT NOT NULL AUTO_INCREMENT,
  lesson_id     INT NOT NULL,
  question      VARCHAR(300) NOT NULL,
  options_json  TEXT NOT NULL,
  correct_index INT NOT NULL,
  PRIMARY KEY (id),
  KEY ix_quiz_lesson (lesson_id),
  CONSTRAINT fk_quiz_lesson FOREIGN KEY (lesson_id) REFERENCES lessons (id)
) ENGINE=InnoDB;

CREATE TABLE enrollments (
  id          INT NOT NULL AUTO_INCREMENT,
  student_id  INT NOT NULL,
  course_id   INT NOT NULL,
  enrolled_at DATETIME NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_enrollments_student_course (student_id, course_id),
  KEY ix_enrollments_course (course_id),
  CONSTRAINT fk_enrollments_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT fk_enrollments_course FOREIGN KEY (course_id) REFERENCES courses (id)
) ENGINE=InnoDB;

CREATE TABLE lesson_progress (
  id           INT NOT NULL AUTO_INCREMENT,
  student_id   INT NOT NULL,
  course_id    INT NOT NULL,
  lesson_id    INT NOT NULL,
  completed_at DATETIME NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_progress_student_lesson (student_id, lesson_id),
  KEY ix_progress_course (course_id),
  KEY ix_progress_lesson (lesson_id),
  CONSTRAINT fk_progress_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT fk_progress_course FOREIGN KEY (course_id) REFERENCES courses (id),
  CONSTRAINT fk_progress_lesson FOREIGN KEY (lesson_id) REFERENCES lessons (id)
) ENGINE=InnoDB;

CREATE TABLE bookmarks (
  id         INT NOT NULL AUTO_INCREMENT,
  student_id INT NOT NULL,
  course_id  INT NOT NULL,
  created_at DATETIME NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_bookmarks_student_course (student_id, course_id),
  KEY ix_bookmarks_course (course_id),
  CONSTRAINT fk_bookmarks_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT fk_bookmarks_course FOREIGN KEY (course_id) REFERENCES courses (id)
) ENGINE=InnoDB;

CREATE TABLE cart_items (
  id         INT NOT NULL AUTO_INCREMENT,
  student_id INT NOT NULL,
  course_id  INT NOT NULL,
  added_at   DATETIME NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_cart_student_course (student_id, course_id),
  KEY ix_cart_course (course_id),
  CONSTRAINT fk_cart_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT fk_cart_course FOREIGN KEY (course_id) REFERENCES courses (id)
) ENGINE=InnoDB;

CREATE TABLE coupons (
  id               INT NOT NULL AUTO_INCREMENT,
  code             VARCHAR(40) NOT NULL,
  discount_type    VARCHAR(10) NOT NULL,
  discount_value   DECIMAL(10,2) NOT NULL,
  expires_on       DATE NOT NULL,
  max_uses         INT NOT NULL,
  used_count       INT NOT NULL DEFAULT 0,
  min_order_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  category         VARCHAR(60) NOT NULL DEFAULT '',
  PRIMARY KEY (id),
  UNIQUE KEY uq_coupons_code (code),
  CONSTRAINT ck_coupons_type CHECK (discount_type IN ('Percent', 'Fixed'))
) ENGINE=InnoDB;

CREATE TABLE orders (
  id              INT NOT NULL AUTO_INCREMENT,
  student_id      INT NOT NULL,
  coupon_code     VARCHAR(40) NULL,
  subtotal        DECIMAL(10,2) NOT NULL,
  bundle_discount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  coupon_discount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  total           DECIMAL(10,2) NOT NULL,
  status          VARCHAR(10) NOT NULL DEFAULT 'Paid',
  created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_orders_student (student_id),
  KEY ix_orders_coupon (coupon_code),
  KEY ix_orders_created (created_at),
  CONSTRAINT fk_orders_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT fk_orders_coupon FOREIGN KEY (coupon_code) REFERENCES coupons (code),
  CONSTRAINT ck_orders_status CHECK (status IN ('Paid', 'Refunded'))
) ENGINE=InnoDB;

CREATE TABLE order_lines (
  id              INT NOT NULL AUTO_INCREMENT,
  order_id        INT NOT NULL,
  course_id       INT NOT NULL,
  price           DECIMAL(10,2) NOT NULL,
  bundle_discount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  coupon_discount DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  final_price     DECIMAL(10,2) NOT NULL,
  PRIMARY KEY (id),
  KEY ix_order_lines_order (order_id),
  KEY ix_order_lines_course (course_id),
  CONSTRAINT fk_order_lines_order FOREIGN KEY (order_id) REFERENCES orders (id),
  CONSTRAINT fk_order_lines_course FOREIGN KEY (course_id) REFERENCES courses (id)
) ENGINE=InnoDB;

CREATE TABLE refunds (
  id          INT NOT NULL AUTO_INCREMENT,
  order_id    INT NOT NULL,
  refunded_at DATETIME NOT NULL,
  amount      DECIMAL(10,2) NOT NULL,
  reason      VARCHAR(200) NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_refunds_order (order_id),
  CONSTRAINT fk_refunds_order FOREIGN KEY (order_id) REFERENCES orders (id)
) ENGINE=InnoDB;

CREATE TABLE reviews (
  id         INT NOT NULL AUTO_INCREMENT,
  course_id  INT NOT NULL,
  student_id INT NOT NULL,
  stars      INT NOT NULL,
  comment    VARCHAR(1000) NOT NULL DEFAULT '',
  created_at DATETIME NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_reviews_course_student (course_id, student_id),
  KEY ix_reviews_student (student_id),
  CONSTRAINT fk_reviews_course FOREIGN KEY (course_id) REFERENCES courses (id),
  CONSTRAINT fk_reviews_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT ck_reviews_stars CHECK (stars BETWEEN 1 AND 5)
) ENGINE=InnoDB;

CREATE TABLE questions (
  id         INT NOT NULL AUTO_INCREMENT,
  course_id  INT NOT NULL,
  student_id INT NOT NULL,
  body       VARCHAR(2000) NOT NULL,
  created_at DATETIME NOT NULL,
  PRIMARY KEY (id),
  KEY ix_questions_course (course_id),
  KEY ix_questions_student (student_id),
  CONSTRAINT fk_questions_course FOREIGN KEY (course_id) REFERENCES courses (id),
  CONSTRAINT fk_questions_student FOREIGN KEY (student_id) REFERENCES students (id)
) ENGINE=InnoDB;

CREATE TABLE answers (
  id            INT NOT NULL AUTO_INCREMENT,
  question_id   INT NOT NULL,
  student_id    INT NULL,
  instructor_id INT NULL,
  body          VARCHAR(2000) NOT NULL,
  is_accepted   TINYINT(1) NOT NULL DEFAULT 0,
  created_at    DATETIME NOT NULL,
  PRIMARY KEY (id),
  KEY ix_answers_question (question_id),
  KEY ix_answers_student (student_id),
  KEY ix_answers_instructor (instructor_id),
  CONSTRAINT fk_answers_question FOREIGN KEY (question_id) REFERENCES questions (id),
  CONSTRAINT fk_answers_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT fk_answers_instructor FOREIGN KEY (instructor_id) REFERENCES instructors (id)
) ENGINE=InnoDB;

CREATE TABLE assignments (
  id          INT NOT NULL AUTO_INCREMENT,
  course_id   INT NOT NULL,
  title       VARCHAR(200) NOT NULL,
  description VARCHAR(1000) NOT NULL DEFAULT '',
  due_at      DATETIME NOT NULL,
  created_at  DATETIME NOT NULL,
  PRIMARY KEY (id),
  KEY ix_assignments_course (course_id),
  CONSTRAINT fk_assignments_course FOREIGN KEY (course_id) REFERENCES courses (id)
) ENGINE=InnoDB;

CREATE TABLE submissions (
  id            INT NOT NULL AUTO_INCREMENT,
  assignment_id INT NOT NULL,
  student_id    INT NOT NULL,
  body          TEXT NOT NULL,
  submitted_at  DATETIME NOT NULL,
  grade         INT NULL,
  feedback      VARCHAR(1000) NULL,
  graded_at     DATETIME NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_submissions_assignment_student (assignment_id, student_id),
  KEY ix_submissions_student (student_id),
  CONSTRAINT fk_submissions_assignment FOREIGN KEY (assignment_id) REFERENCES assignments (id),
  CONSTRAINT fk_submissions_student FOREIGN KEY (student_id) REFERENCES students (id),
  CONSTRAINT ck_submissions_grade CHECK (grade IS NULL OR grade BETWEEN 0 AND 100)
) ENGINE=InnoDB;
