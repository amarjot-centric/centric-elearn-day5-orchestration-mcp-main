import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getCourse, enroll, addToCart, formatPrice, markLessonComplete, getProgress, getSavedName, saveName, stars, getCourseAssignments } from '../api.js'
import ReviewsSection from '../components/ReviewsSection.jsx'
import QaSection from '../components/QaSection.jsx'

export default function CourseDetailPage() {
  const { id } = useParams()
  const [course, setCourse] = useState(null)
  const [name, setName] = useState(getSavedName())
  const [message, setMessage] = useState(null) // { type: 'ok' | 'error', text }
  const [error, setError] = useState('')
  const [doneIds, setDoneIds] = useState([])
  const [assignments, setAssignments] = useState([])

  useEffect(() => {
    getCourse(id).then(setCourse).catch((e) => setError(e.message))
  }, [id])

  useEffect(() => {
    getCourseAssignments(id).then(setAssignments).catch(() => {})
  }, [id])

  const reloadCourse = () => getCourse(id).then(setCourse).catch(() => {})

  // Load which lessons the saved student has finished.
  useEffect(() => {
    if (!getSavedName()) return
    getProgress(getSavedName())
      .then((list) => setDoneIds(list.find((p) => p.courseId === Number(id))?.completedLessonIds || []))
      .catch(() => {})
  }, [id])

  async function handleComplete(lessonId) {
    try {
      await markLessonComplete(name, course.id, lessonId)
      saveName(name.trim())
      setDoneIds((ids) => (ids.includes(lessonId) ? ids : [...ids, lessonId]))
      setMessage({ type: 'ok', text: 'Lesson marked complete.' })
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    }
  }

  async function handleEnroll(e) {
    e.preventDefault()
    try {
      if (course.price > 0) {
        await addToCart(name, course.id)
        saveName(name.trim())
        setMessage({ type: 'ok', text: 'Added to your cart.' })
        return
      }
      await enroll(course.id, name)
      saveName(name.trim())
      setMessage({ type: 'ok', text: 'You are enrolled!' })
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    }
  }

  if (error) return <p className="message error">{error}</p>
  if (!course) return <p>Loading...</p>

  return (
    <>
      <Link to="/">&larr; All courses</Link>
      <h1>{course.title}</h1>
      <p>{course.shortDescription}</p>
      <p className="meta">
        <span>{course.instructor}</span>
        <span>{course.category} · {course.level}</span>
        <span>{formatPrice(course.price)}</span>
        {course.reviewCount > 0 && <span>{stars(course.averageRating)} {course.averageRating.toFixed(1)} ({course.reviewCount})</span>}
      </p>
      {course.prerequisites.length > 0 && (
        <p className="muted">
          Finish first: {course.prerequisites.map((p, i) => (
            <span key={p.id}>{i > 0 && ', '}<Link to={`/courses/${p.id}`}>{p.title}</Link></span>
          ))}
        </p>
      )}

      <form className="panel" onSubmit={handleEnroll}>
        <h2>{course.price > 0 ? 'Buy this course' : 'Enroll'}</h2>
        <div className="row">
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Your name"
          />
          <button type="submit">{course.price > 0 ? 'Add to cart' : 'Enroll'}</button>
        </div>
        {message && <p className={`message ${message.type}`}>{message.text}</p>}
      </form>

      <h2>Lessons</h2>
      <ol className="lessons">
        {course.lessons.map((l) => (
          <li key={l.id}>
            <strong>{l.title}</strong>
            <span className="muted"> · {l.durationMinutes} min</span>
            <p>{l.content}</p>
            <div className="row">
              <Link to={`/lessons/${l.id}/quiz`}>Take quiz</Link>
              {doneIds.includes(l.id)
                ? <span className="tag">Completed</span>
                : <button type="button" onClick={() => handleComplete(l.id)}>Mark complete</button>}
            </div>
          </li>
        ))}
      </ol>

      {assignments.length > 0 && (
        <>
          <h2>Assignments</h2>
          <ul>
            {assignments.map((a) => (
              <li key={a.id}><strong>{a.title}</strong> <span className="muted">due {new Date(a.dueAt).toLocaleDateString()}</span></li>
            ))}
          </ul>
          <Link to="/assignments">Go to my assignments</Link>
        </>
      )}

      <ReviewsSection courseId={course.id} onChanged={reloadCourse} />
      <QaSection courseId={course.id} instructorId={course.instructorId} />
    </>
  )
}
