import { useEffect, useState } from 'react'
import { getCourses, getBookmarks, addBookmark, removeBookmark, addToCart, getSavedName } from '../api.js'
import CourseCard from '../components/CourseCard.jsx'

export default function CoursesPage() {
  const [courses, setCourses] = useState([])
  const [search, setSearch] = useState('')
  const [bookmarkedIds, setBookmarkedIds] = useState([])
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    getCourses(search)
      .then((data) => {
        setCourses(data)
        setError('')
      })
      .catch((e) => setError(e.message))
  }, [search])

  // Load the saved student's bookmarks, if a name was saved.
  useEffect(() => {
    const name = getSavedName()
    if (!name) return
    getBookmarks(name)
      .then((list) => setBookmarkedIds(list.map((b) => b.courseId)))
      .catch(() => {})
  }, [])

  async function handleToggleBookmark(courseId, isBookmarked) {
    const name = getSavedName()
    if (!name) {
      setError('Enter your name first (open My Courses and show your courses), then bookmark.')
      return
    }
    try {
      if (isBookmarked) {
        await removeBookmark(name, courseId)
        setBookmarkedIds((ids) => ids.filter((id) => id !== courseId))
      } else {
        await addBookmark(name, courseId)
        setBookmarkedIds((ids) => [...ids, courseId])
      }
      setError('')
    } catch (err) {
      setError(err.message)
    }
  }

  async function handleAddToCart(courseId) {
    const name = getSavedName()
    if (!name) {
      setError('Enter your name first (open My Courses and show your courses), then add to cart.')
      return
    }
    try {
      await addToCart(name, courseId)
      setMessage('Added to your cart.')
      setError('')
    } catch (err) {
      setMessage('')
      setError(err.message)
    }
  }

  return (
    <>
      <h1>Courses</h1>
      <div className="row">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search courses by title"
        />
      </div>
      {error && <p className="message error">{error}</p>}
      {message && <p className="message ok">{message}</p>}
      {courses.length === 0 && !error && <p className="muted">No courses found.</p>}
      <div className="grid">
        {courses.map((c) => (
          <CourseCard
            key={c.id}
            course={c}
            bookmarked={bookmarkedIds.includes(c.id)}
            onToggleBookmark={handleToggleBookmark}
            onAddToCart={handleAddToCart}
          />
        ))}
      </div>
    </>
  )
}
