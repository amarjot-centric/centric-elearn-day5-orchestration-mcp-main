import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getEnrollments, getSavedName, saveName } from '../api.js'

export default function MyCoursesPage() {
  const [name, setName] = useState(getSavedName())
  const [enrollments, setEnrollments] = useState([])
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')

  async function load(studentName) {
    if (!studentName.trim()) return
    try {
      setEnrollments(await getEnrollments(studentName))
      setSearched(true)
      setError('')
      saveName(studentName.trim())
    } catch (err) {
      setError(err.message)
    }
  }

  // If a name was saved, show its courses right away.
  useEffect(() => {
    load(name)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function handleSubmit(e) {
    e.preventDefault()
    load(name)
  }

  return (
    <>
      <h1>My Courses</h1>
      <form className="row" onSubmit={handleSubmit}>
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Your name"
        />
        <button type="submit">Show my courses</button>
      </form>

      {error && <p className="message error">{error}</p>}
      {searched && enrollments.length === 0 && (
        <p className="muted">No courses yet. Pick one from the Courses page.</p>
      )}
      <ul className="enrollments">
        {enrollments.map((e) => (
          <li key={e.id} className="panel">
            <Link to={`/courses/${e.courseId}`}>{e.courseTitle}</Link>
          </li>
        ))}
      </ul>
    </>
  )
}
