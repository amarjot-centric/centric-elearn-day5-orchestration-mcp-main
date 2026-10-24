import { useEffect, useState } from 'react'
import { getMyAssignments, submitAssignment, getSavedName, saveName } from '../api.js'

export default function AssignmentsPage() {
  const [name, setName] = useState(getSavedName())
  const [items, setItems] = useState([])
  const [loaded, setLoaded] = useState(false)
  const [error, setError] = useState('')
  const [drafts, setDrafts] = useState({})     // assignmentId -> text
  const [messages, setMessages] = useState({}) // assignmentId -> { type, text }

  async function load(studentName) {
    if (!studentName.trim()) return
    try {
      setItems(await getMyAssignments(studentName.trim()))
      saveName(studentName.trim())
      setLoaded(true)
      setError('')
    } catch (err) {
      setError(err.message)
    }
  }

  useEffect(() => {
    load(name)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function handleSubmit(e, id) {
    e.preventDefault()
    try {
      await submitAssignment(id, name, drafts[id] || '')
      setMessages((m) => ({ ...m, [id]: { type: 'ok', text: 'Submitted.' } }))
      load(name)
    } catch (err) {
      setMessages((m) => ({ ...m, [id]: { type: 'error', text: err.message } }))
    }
  }

  return (
    <>
      <h1>My Assignments</h1>
      <form className="row" onSubmit={(e) => { e.preventDefault(); load(name) }}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <button type="submit">Show my assignments</button>
      </form>
      {error && <p className="message error">{error}</p>}
      {loaded && items.length === 0 && <p className="muted">No assignments in your courses.</p>}

      {items.map((a) => {
        const overdue = !a.submitted && new Date(a.dueAt) < new Date()
        return (
          <div key={a.assignmentId} className="panel">
            <h3>{a.title} <span className="muted">· {a.courseTitle}</span></h3>
            <p>{a.description}</p>
            <p className={overdue ? 'pill-late' : 'muted'}>Due {new Date(a.dueAt).toLocaleString()}{overdue ? ' (overdue)' : ''}</p>
            {a.grade != null && (
              <p><strong>Grade: {a.grade}/100</strong>{a.feedback ? ` · ${a.feedback}` : ''}</p>
            )}
            {a.grade == null && (
              <form onSubmit={(e) => handleSubmit(e, a.assignmentId)} className="form-stack">
                <textarea
                  rows={3}
                  value={drafts[a.assignmentId] ?? a.submissionBody ?? ''}
                  onChange={(e) => setDrafts((d) => ({ ...d, [a.assignmentId]: e.target.value }))}
                  placeholder="Write your answer here"
                />
                <button type="submit">{a.submitted ? 'Update submission' : 'Submit'}</button>
              </form>
            )}
            {messages[a.assignmentId] && <p className={`message ${messages[a.assignmentId].type}`}>{messages[a.assignmentId].text}</p>}
          </div>
        )
      })}
    </>
  )
}
