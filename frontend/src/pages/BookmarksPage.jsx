import { useEffect, useState } from 'react'
import { getBookmarks, removeBookmark, getSavedName, saveName } from '../api.js'
import CourseCard from '../components/CourseCard.jsx'

export default function BookmarksPage() {
  const [name, setName] = useState(getSavedName())
  const [bookmarks, setBookmarks] = useState([])
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')

  async function load(studentName) {
    if (!studentName.trim()) return
    try {
      setBookmarks(await getBookmarks(studentName))
      setSearched(true)
      setError('')
      saveName(studentName.trim())
    } catch (err) {
      setError(err.message)
    }
  }

  useEffect(() => {
    load(name)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function handleSubmit(e) {
    e.preventDefault()
    load(name)
  }

  async function handleRemove(courseId) {
    try {
      await removeBookmark(name, courseId)
      setBookmarks((list) => list.filter((b) => b.courseId !== courseId))
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <>
      <h1>My Bookmarks</h1>
      <form className="row" onSubmit={handleSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <button type="submit">Show my bookmarks</button>
      </form>

      {error && <p className="message error">{error}</p>}
      {searched && bookmarks.length === 0 && (
        <p className="muted">No bookmarks yet. Use the star on a course card.</p>
      )}
      <div className="grid">
        {bookmarks.map((b) => (
          <CourseCard
            key={b.id}
            course={{ id: b.courseId, title: b.courseTitle, shortDescription: b.shortDescription,
              category: b.category, instructor: b.instructor, level: b.level, lessonCount: b.lessonCount }}
            bookmarked
            onToggleBookmark={handleRemove}
          />
        ))}
      </div>
    </>
  )
}
