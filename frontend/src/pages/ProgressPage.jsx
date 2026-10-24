import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getProgress, getSavedName, saveName } from '../api.js'

export default function ProgressPage() {
  const [name, setName] = useState(getSavedName())
  const [items, setItems] = useState([])
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')

  async function load(studentName) {
    if (!studentName.trim()) return
    try {
      setItems(await getProgress(studentName))
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

  return (
    <>
      <h1>My Progress</h1>
      <form className="row" onSubmit={handleSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <button type="submit">Show my progress</button>
      </form>

      {error && <p className="message error">{error}</p>}
      {searched && items.length === 0 && (
        <p className="muted">No courses yet. Enroll in a course first.</p>
      )}
      {items.map((p) => (
        <div key={p.courseId} className="panel">
          <Link to={`/courses/${p.courseId}`}>{p.courseTitle}</Link>
          <div className="progress-track">
            <div className="progress-fill" style={{ width: `${p.percent}%` }} />
          </div>
          <span className="muted">{p.completedLessons} of {p.totalLessons} lessons · {p.percent}%</span>
        </div>
      ))}
    </>
  )
}
