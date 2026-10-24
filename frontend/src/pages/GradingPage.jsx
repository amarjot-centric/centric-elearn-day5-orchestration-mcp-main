import { useEffect, useState } from 'react'
import { getInstructors, getInstructorAssignments, getSubmissions, gradeSubmission } from '../api.js'

export default function GradingPage() {
  const [instructors, setInstructors] = useState([])
  const [id, setId] = useState('')
  const [assignments, setAssignments] = useState([])
  const [openId, setOpenId] = useState(null)
  const [submissions, setSubmissions] = useState([])
  const [form, setForm] = useState({}) // submissionId -> { grade, feedback }
  const [message, setMessage] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    getInstructors()
      .then((list) => {
        setInstructors(list)
        if (list.length > 0) setId(String(list[0].id))
      })
      .catch((e) => setError(e.message))
  }, [])

  useEffect(() => {
    if (!id) return
    getInstructorAssignments(id)
      .then((list) => { setAssignments(list); setOpenId(null); setSubmissions([]) })
      .catch((e) => setError(e.message))
  }, [id])

  async function open(assignmentId) {
    setOpenId(assignmentId)
    setMessage(null)
    try {
      setSubmissions(await getSubmissions(assignmentId))
    } catch (err) {
      setError(err.message)
    }
  }

  async function handleGrade(e, submission) {
    e.preventDefault()
    const f = form[submission.id] || {}
    try {
      await gradeSubmission(submission.id, Number(f.grade ?? submission.grade), f.feedback ?? submission.feedback ?? '')
      setMessage({ type: 'ok', text: `Saved the grade for ${submission.studentName}.` })
      setSubmissions(await getSubmissions(openId))
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    }
  }

  return (
    <>
      <h1>Grading</h1>
      <div className="row">
        <select value={id} onChange={(e) => setId(e.target.value)} aria-label="Instructor">
          {instructors.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
        </select>
      </div>
      {error && <p className="message error">{error}</p>}

      <div className="panel">
        <h2>Assignments</h2>
        {assignments.length === 0 && <p className="muted">No assignments.</p>}
        <ul>
          {assignments.map((a) => (
            <li key={a.id}>
              <button type="button" onClick={() => open(a.id)}>Open</button>{' '}
              <strong>{a.title}</strong> <span className="muted">· {a.courseTitle} · due {new Date(a.dueAt).toLocaleDateString()} · {a.submissionCount} submissions</span>
            </li>
          ))}
        </ul>
      </div>

      {openId && (
        <div className="panel">
          <h2>Submissions</h2>
          {message && <p className={`message ${message.type}`}>{message.text}</p>}
          {submissions.length === 0 && <p className="muted">Nothing submitted yet.</p>}
          {submissions.map((s) => (
            <form key={s.id} className="form-stack review" onSubmit={(e) => handleGrade(e, s)}>
              <div><strong>{s.studentName}</strong> <span className="muted">· {new Date(s.submittedAt).toLocaleString()}</span></div>
              <div>{s.body}</div>
              <div className="row">
                <input
                  type="number" min="0" max="100" placeholder="Grade 0-100"
                  defaultValue={s.grade ?? ''}
                  onChange={(e) => setForm((f) => ({ ...f, [s.id]: { ...f[s.id], grade: e.target.value } }))}
                />
                <input
                  placeholder="Feedback"
                  defaultValue={s.feedback ?? ''}
                  onChange={(e) => setForm((f) => ({ ...f, [s.id]: { ...f[s.id], feedback: e.target.value } }))}
                />
                <button type="submit">{s.grade != null ? 'Update grade' : 'Save grade'}</button>
              </div>
            </form>
          ))}
        </div>
      )}
    </>
  )
}
