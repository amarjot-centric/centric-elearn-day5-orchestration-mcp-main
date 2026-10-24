import { useState } from 'react'
import { Link } from 'react-router-dom'
import { createCourse } from '../api.js'

const emptyLesson = { title: '', durationMinutes: 10, content: '' }
const emptyForm = {
  title: '', shortDescription: '', category: '', instructor: '', level: 'Beginner', price: 0,
}

export default function InstructorPage() {
  const [form, setForm] = useState(emptyForm)
  const [lessons, setLessons] = useState([{ ...emptyLesson }])
  const [created, setCreated] = useState(null)
  const [error, setError] = useState('')

  const setField = (field) => (e) => setForm({ ...form, [field]: e.target.value })

  function setLesson(index, field, value) {
    setLessons(lessons.map((l, i) => (i === index ? { ...l, [field]: value } : l)))
  }

  async function handleSubmit(e) {
    e.preventDefault()
    try {
      const course = await createCourse({
        ...form,
        price: Number(form.price) || 0,
        lessons: lessons.map((l) => ({ ...l, durationMinutes: Number(l.durationMinutes) })),
      })
      setCreated(course)
      setError('')
      setForm(emptyForm)
      setLessons([{ ...emptyLesson }])
    } catch (err) {
      setCreated(null)
      setError(err.message)
    }
  }

  return (
    <>
      <h1>Instructor</h1>
      <form className="panel form-stack" onSubmit={handleSubmit}>
        <h2>Add a course</h2>
        <input value={form.title} onChange={setField('title')} placeholder="Title" />
        <input value={form.shortDescription} onChange={setField('shortDescription')} placeholder="Short description" />
        <input value={form.category} onChange={setField('category')} placeholder="Category (for example Programming)" />
        <input value={form.instructor} onChange={setField('instructor')} placeholder="Instructor name" />
        <input type="number" min="0" step="0.01" value={form.price} onChange={setField('price')} placeholder="Price (0 = free)" />
        <select value={form.level} onChange={setField('level')}>
          <option>Beginner</option>
          <option>Intermediate</option>
          <option>Advanced</option>
        </select>

        <h2>Lessons</h2>
        {lessons.map((l, i) => (
          <div key={i} className="lesson-fields">
            <input value={l.title} onChange={(e) => setLesson(i, 'title', e.target.value)} placeholder={`Lesson ${i + 1} title`} />
            <input
              type="number" min="1" value={l.durationMinutes}
              onChange={(e) => setLesson(i, 'durationMinutes', e.target.value)}
              placeholder="Minutes"
            />
            <textarea value={l.content} onChange={(e) => setLesson(i, 'content', e.target.value)} placeholder="Lesson content" rows={2} />
            {lessons.length > 1 && (
              <button type="button" onClick={() => setLessons(lessons.filter((_, j) => j !== i))}>Remove lesson</button>
            )}
          </div>
        ))}
        <div className="row">
          <button type="button" onClick={() => setLessons([...lessons, { ...emptyLesson }])}>Add lesson</button>
          <button type="submit">Create course</button>
        </div>

        {error && <p className="message error">{error}</p>}
        {created && (
          <p className="message ok">
            Course created: <Link to={`/courses/${created.id}`}>{created.title}</Link>
          </p>
        )}
      </form>
    </>
  )
}
