import { useEffect, useState } from 'react'
import { getInstructorRevenue, getPayouts } from '../api.js'

const money = (value) => `$${Number(value).toFixed(2)}`

export default function InstructorRevenuePage() {
  const [rows, setRows] = useState([])
  const [error, setError] = useState('')
  const [month, setMonth] = useState('2026-09')
  const [payouts, setPayouts] = useState(null)

  useEffect(() => {
    getInstructorRevenue()
      .then((data) => {
        setRows(data)
        setError('')
      })
      .catch((e) => setError(e.message))
  }, [])

  function loadPayouts(e) {
    e.preventDefault()
    getPayouts(month)
      .then((data) => { setPayouts(data); setError('') })
      .catch((err) => { setPayouts(null); setError(err.message) })
  }

  return (
    <>
      <h1>Instructor Revenue</h1>
      <p className="muted">Each instructor earns 70% of sales that were not refunded.</p>
      {error && <p className="message error">{error}</p>}
      <div className="panel">
        <table className="cart-table">
          <thead>
            <tr>
              <th>Instructor</th><th className="num">Gross sales</th>
              <th className="num">Refunds</th><th className="num">Instructor share</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.instructor}>
                <td>{r.instructor}</td>
                <td className="num">{money(r.grossSales)}</td>
                <td className="num">{money(r.refunds)}</td>
                <td className="num">{money(r.instructorShare)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    
      <h2>Monthly payouts</h2>
      <form className="row" onSubmit={loadPayouts}>
        <input value={month} onChange={(e) => setMonth(e.target.value)} placeholder="YYYY-MM" aria-label="Month" />
        <button type="submit">Show payouts</button>
      </form>
      {payouts && (
        <div className="panel">
          <table className="cart-table">
            <thead>
              <tr><th>Instructor</th><th className="num">Lines</th><th className="num">Gross</th><th className="num">Payout</th></tr>
            </thead>
            <tbody>
              {payouts.rows.map((r) => (
                <tr key={r.instructorId}>
                  <td>{r.instructor}</td><td className="num">{r.lines}</td>
                  <td className="num">{money(r.gross)}</td><td className="num">{money(r.share)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr><td>Total {payouts.month}</td><td></td><td className="num">{money(payouts.totalGross)}</td><td className="num">{money(payouts.totalShare)}</td></tr>
            </tfoot>
          </table>
        </div>
      )}
    </>
  )
}
