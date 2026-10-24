import { useEffect, useState } from 'react'
import { getOrders, refundOrder, getSavedName, saveName } from '../api.js'

const money = (value) => `$${Number(value).toFixed(2)}`

export default function MyOrdersPage() {
  const [name, setName] = useState(getSavedName())
  const [orders, setOrders] = useState([])
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')
  const [refundErrors, setRefundErrors] = useState({}) // orderId -> reason

  async function load(studentName) {
    if (!studentName.trim()) return
    try {
      setOrders(await getOrders(studentName))
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

  async function handleRefund(orderId) {
    try {
      await refundOrder(orderId)
      setRefundErrors((r) => ({ ...r, [orderId]: '' }))
      load(name)
    } catch (err) {
      setRefundErrors((r) => ({ ...r, [orderId]: err.message }))
    }
  }

  return (
    <>
      <h1>My Orders</h1>
      <form className="row" onSubmit={handleSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <button type="submit">Show my orders</button>
      </form>

      {error && <p className="message error">{error}</p>}
      {searched && orders.length === 0 && <p className="muted">No orders yet.</p>}
      {orders.map((o) => (
        <div key={o.id} className="panel">
          <div className="row">
            <strong>Order {o.id}</strong>
            <span className="muted">{new Date(o.createdAt).toLocaleDateString()}</span>
            <span className={`order-status ${o.status.toLowerCase()}`}>{o.status}</span>
            <strong>{money(o.total)}</strong>
          </div>
          <ul>
            {o.lines.map((l) => (
              <li key={l.courseId}>{l.title} - {money(l.final)}</li>
            ))}
          </ul>
          {o.couponCode && <p className="muted">Coupon: {o.couponCode}</p>}
          {o.status === 'Paid' && (
            <button type="button" onClick={() => handleRefund(o.id)}>Refund</button>
          )}
          {refundErrors[o.id] && <p className="message error">{refundErrors[o.id]}</p>}
        </div>
      ))}
    </>
  )
}
