import { useEffect, useState } from 'react'
import { acceptAnswer, answerQuestion, askQuestion, getInstructors, getQuestions, getSavedName, saveName } from '../api.js'

export default function QaSection({ courseId, instructorId }) {
  const [questions, setQuestions] = useState([])
  const [name, setName] = useState(getSavedName())
  const [body, setBody] = useState('')
  const [replies, setReplies] = useState({}) // questionId -> text
  const [asInstructor, setAsInstructor] = useState(false)
  const [instructorName, setInstructorName] = useState('')
  const [message, setMessage] = useState(null)

  const load = () => getQuestions(courseId).then(setQuestions).catch(() => {})

  useEffect(() => {
    load()
    getInstructors().then((list) => setInstructorName(list.find((i) => i.id === instructorId)?.name || '')).catch(() => {})
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [courseId])

  async function handleAsk(e) {
    e.preventDefault()
    try {
      await askQuestion(courseId, name, body)
      saveName(name.trim())
      setBody('')
      setMessage(null)
      load()
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    }
  }

  async function handleReply(e, questionId) {
    e.preventDefault()
    try {
      const payload = asInstructor
        ? { instructorId, body: replies[questionId] }
        : { studentName: name, body: replies[questionId] }
      await answerQuestion(questionId, payload)
      setReplies((r) => ({ ...r, [questionId]: '' }))
      setMessage(null)
      load()
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    }
  }

  async function handleAccept(answerId) {
    try {
      await acceptAnswer(answerId, instructorId)
      load()
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    }
  }

  return (
    <section>
      <h2>Questions and answers</h2>
      <label className="muted">
        <input type="checkbox" checked={asInstructor} onChange={(e) => setAsInstructor(e.target.checked)} />
        {' '}Reply as the instructor{instructorName ? ` (${instructorName})` : ''}
      </label>
      <form className="panel form-stack" onSubmit={handleAsk}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <textarea rows={2} value={body} onChange={(e) => setBody(e.target.value)} placeholder="Ask a question about this course" />
        <button type="submit">Ask</button>
      </form>
      {message && <p className={`message ${message.type}`}>{message.text}</p>}
      {questions.length === 0 && <p className="muted">No questions yet.</p>}
      {questions.slice(0, 20).map((q) => (
        <div key={q.id} className="question">
          <strong>{q.body}</strong>
          <div className="muted">{q.studentName} · {new Date(q.createdAt).toLocaleDateString()}</div>
          {q.answers.map((a) => (
            <div key={a.id} className={`answer${a.isAccepted ? ' accepted' : ''}`}>
              {a.body}
              <div className="muted">
                {a.authorName} ({a.authorRole}){a.isAccepted ? ' · Accepted answer' : ''}
                {asInstructor && !a.isAccepted && (
                  <> · <button type="button" onClick={() => handleAccept(a.id)}>Accept</button></>
                )}
              </div>
            </div>
          ))}
          <form className="row" onSubmit={(e) => handleReply(e, q.id)}>
            <input value={replies[q.id] || ''} onChange={(e) => setReplies((r) => ({ ...r, [q.id]: e.target.value }))} placeholder="Write an answer" />
            <button type="submit">Reply</button>
          </form>
        </div>
      ))}
      {questions.length > 20 && <p className="muted">Showing the 20 newest of {questions.length} questions.</p>}
    </section>
  )
}
