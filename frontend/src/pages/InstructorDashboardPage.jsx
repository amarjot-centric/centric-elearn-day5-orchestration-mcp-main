import { useEffect, useState } from 'react'
import { getInstructors, getInstructorDashboard } from '../api.js'

const money = (value) => `$${Number(value).toFixed(2)}`

export default function InstructorDashboardPage() {
  const [instructors, setInstructors] = useState([])
  const [id, setId] = useState('')
  const [data, setData] = useState(null)
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
    getInstructorDashboard(id)
      .then((d) => { setData(d); setError('') })
      .catch((e) => setError(e.message))
  }, [id])

  return (
    <>
      <h1>Instructor Dashboard</h1>
      <div className="row">
        <select value={id} onChange={(e) => setId(e.target.value)} aria-label="Instructor">
          {instructors.map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
        </select>
      </div>
      {error && <p className="message error">{error}</p>}

      {data && (
        <>
          <div className="stat-grid">
            <div className="stat"><div className="label">Courses</div><div className="value">{data.courseCount}</div></div>
            <div className="stat"><div className="label">Enrollments</div><div className="value">{data.enrollments}</div></div>
            <div className="stat"><div className="label">Revenue (70%)</div><div className="value">{money(data.revenue)}</div></div>
            <div className="stat"><div className="label">Average rating</div><div className="value">{data.averageRating.toFixed(2)}</div></div>
            <div className="stat"><div className="label">Completion rate</div><div className="value">{data.completionRate.toFixed(1)}%</div></div>
          </div>
          <p className="muted">Gross sales {money(data.grossSales)}, refunds {money(data.refunds)}, {data.reviewCount} reviews.</p>

          <div className="panel">
            <h2>Courses</h2>
            <table className="cart-table">
              <thead>
                <tr><th>Course</th><th className="num">Enrollments</th><th className="num">Rating</th><th className="num">Reviews</th><th className="num">Completion</th></tr>
              </thead>
              <tbody>
                {data.courses.map((c) => (
                  <tr key={c.courseId}>
                    <td>{c.title}</td>
                    <td className="num">{c.enrollments}</td>
                    <td className="num">{c.reviewCount ? c.averageRating.toFixed(2) : '-'}</td>
                    <td className="num">{c.reviewCount}</td>
                    <td className="num">{c.completionRate.toFixed(1)}%</td>
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
