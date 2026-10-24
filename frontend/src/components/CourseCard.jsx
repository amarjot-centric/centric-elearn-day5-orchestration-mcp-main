import { Link } from 'react-router-dom'
import { formatPrice, stars } from '../api.js'

// onToggleBookmark is optional. When given, a bookmark button is shown.
// onAddToCart is optional. When given, paid courses show an "Add to cart" button.
export default function CourseCard({ course, bookmarked = false, onToggleBookmark, onAddToCart }) {
  function handleAdd(e) {
    e.preventDefault()
    e.stopPropagation()
    onAddToCart(course.id)
  }

  function handleBookmark(e) {
    e.preventDefault() // the card is a link; do not open the course
    e.stopPropagation()
    onToggleBookmark(course.id, bookmarked)
  }

  return (
    <Link to={`/courses/${course.id}`} className="card">
      <div className="card-top">
        <span className="tag">{course.category}</span>
        {onToggleBookmark && (
          <button
            type="button"
            className={`bookmark-btn${bookmarked ? ' on' : ''}`}
            onClick={handleBookmark}
            aria-pressed={bookmarked}
            aria-label={bookmarked ? 'Remove bookmark' : 'Add bookmark'}
            title={bookmarked ? 'Remove bookmark' : 'Bookmark this course'}
          >
            {bookmarked ? '★' : '☆'}
          </button>
        )}
      </div>
      <h3>{course.title}</h3>
      <p>{course.shortDescription}</p>
      <div className="meta">
        <span>{course.instructor}</span>
        <span>{course.level} · {course.lessonCount} lessons</span>
      </div>
      {course.reviewCount > 0 && (
        <span className="muted">{stars(course.averageRating)} {course.averageRating.toFixed(1)} ({course.reviewCount})</span>
      )}
      <div className="price-row">
        <span className={course.price > 0 ? 'price' : 'price free'}>{formatPrice(course.price)}</span>
        {course.price > 0 && onAddToCart && (
          <button type="button" onClick={handleAdd}>Add to cart</button>
        )}
      </div>
    </Link>
  )
}
