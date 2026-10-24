import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getStudentDashboard, getSavedName, saveName } from '../api.js'

const money = (value) => `$${Number(value).toFixed(2)}`

export default function StudentDashboardPage() {
  const [name, setName] = useState(getSavedName())
  const [data, setData] = useState(null)
  const [error, setError] = useState('')

  async function load(studentName) {
    if (!studentName.trim()) return
    try {
      setData(await getStudentDashboard(studentName.trim()))
      saveName(studentName.trim())
      setError('')
    } catch (err) {
      setData(null)
      setError(err.message)
    }
  }

  useEffect(() => {
    load(name)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <>
      <h1>My Dashboard</h1>
      <form className="row" onSubmit={(e) => { e.preventDefault(); load(name) }}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <button type="submit">Show my dashboard</button>
      </form>
      {error && <p className="message error">{error}</p>}

      {data && (
        <>
          <div className="stat-grid">
            <div className="stat"><div className="label">In progress</div><div className="value">{data.inProgress.length}</div></div>
            <div className="stat"><div className="label">Certificates</div><div className="value">{data.certificates.length}</div></div>
            <div className="stat"><div className="label">Orders</div><div className="value">{data.orders.length}</div></div>
            <div className="stat"><div className="label">Deadlines ahead</div><div className="value">{data.upcomingAssignments.length}</div></div>
          </div>

          <div className="panel">
            <h2>Courses in progress</h2>
            {data.inProgress.length === 0 && <p className="muted">Nothing in progress.</p>}
            {data.inProgress.map((p) => (
              <div key={p.courseId}>
                <Link to={`/courses/${p.courseId}`}>{p.courseTitle}</Link>
                <span className="muted"> · {p.completedLessons}/{p.totalLessons} lessons</span>
                <div className="progress-track"><div className="progress-fill" style={{ width: `${p.percent}%` }} /></div>
              </div>
            ))}
          </div>

          <div className="panel">
            <h2>Upcoming assignment deadlines</h2>
            {data.upcomingAssignments.length === 0 && <p className="muted">No open deadlines.</p>}
            <ul>
              {data.upcomingAssignments.map((a) => (
                <li key={a.assignmentId}>
                  <strong>{a.title}</strong> <span className="muted">({a.courseTitle})</span> due {new Date(a.dueAt).toLocaleString()}
                </li>
              ))}
            </ul>
            <Link to="/assignments">Open my assignments</Link>
          </div>

          <div className="panel">
            <h2>Certificates</h2>
            {data.certificates.length === 0 && <p className="muted">No certificates yet.</p>}
            <ul>
              {data.certificates.map((c) => (
                <li key={c.certificateId}>{c.courseTitle} <span className="muted">· {c.certificateId}</span></li>
              ))}
            </ul>
          </div>

          <div className="panel">
            <h2>Orders</h2>
            {data.orders.length === 0 && <p className="muted">No orders yet.</p>}
            <table className="cart-table">
              <tbody>
                {data.orders.map((o) => (
                  <tr key={o.id}>
                    <td>Order {o.id}</td>
                    <td>{new Date(o.createdAt).toLocaleDateString()}</td>
                    <td className={`order-status ${o.status.toLowerCase()}`}>{o.status}</td>
                    <td className="num">{money(o.total)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </>
  )
}
