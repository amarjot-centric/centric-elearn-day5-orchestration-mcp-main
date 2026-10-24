import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getCart, removeFromCart, previewCheckout, checkout, getSavedName, saveName } from '../api.js'

const money = (value) => `$${Number(value).toFixed(2)}`

export default function CartPage() {
  const [name, setName] = useState(getSavedName())
  const [coupon, setCoupon] = useState('')
  const [preview, setPreview] = useState(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  // The preview comes from the backend, so the page never does the price maths itself.
  async function load(studentName, couponCode) {
    if (!studentName.trim()) return
    try {
      const items = await getCart(studentName)
      setPreview(items.length === 0 ? { lines: [] } : await previewCheckout(studentName, couponCode))
      setError('')
      saveName(studentName.trim())
    } catch (err) {
      setError(err.message)
    }
  }

  useEffect(() => {
    load(name, coupon)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function handleSubmit(e) {
    e.preventDefault()
    setMessage('')
    load(name, coupon)
  }

  async function handleRemove(courseId) {
    try {
      await removeFromCart(name, courseId)
      load(name, coupon)
    } catch (err) {
      setError(err.message)
    }
  }

  async function handlePay() {
    try {
      const order = await checkout(name, coupon)
      setMessage(`Thank you! Order ${order.id} is paid (${money(order.total)}). You are enrolled.`)
      setCoupon('')
      setError('')
      load(name, '')
    } catch (err) {
      setMessage('')
      setError(err.message)
    }
  }

  const lines = preview?.lines || []

  return (
    <>
      <h1>Cart</h1>
      <form className="row" onSubmit={handleSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <input value={coupon} onChange={(e) => setCoupon(e.target.value)} placeholder="Coupon code" />
        <button type="submit">Show my cart</button>
      </form>

      {error && <p className="message error">{error}</p>}
      {message && <p className="message ok">{message}</p>}
      {lines.length === 0 && !error && (
        <p className="muted">Your cart is empty. Pick a paid course from the <Link to="/">Courses</Link> page.</p>
      )}

      {lines.length > 0 && (
        <div className="panel">
          <table className="cart-table">
            <thead>
              <tr>
                <th>Course</th><th className="num">Price</th><th className="num">Bundle</th>
                <th className="num">Coupon</th><th className="num">Final</th><th></th>
              </tr>
            </thead>
            <tbody>
              {lines.map((l) => (
                <tr key={l.courseId}>
                  <td>{l.title}</td>
                  <td className="num">{money(l.price)}</td>
                  <td className="num">-{money(l.bundleDiscount)}</td>
                  <td className="num">-{money(l.couponDiscount)}</td>
                  <td className="num">{money(l.final)}</td>
                  <td><button type="button" onClick={() => handleRemove(l.courseId)}>Remove</button></td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr><td>Subtotal</td><td className="num" colSpan={4}>{money(preview.subtotal)}</td><td></td></tr>
              <tr><td>Bundle discount (3 or more courses: 15% off)</td><td className="num" colSpan={4}>-{money(preview.bundleDiscount)}</td><td></td></tr>
              <tr><td>Coupon discount</td><td className="num" colSpan={4}>-{money(preview.couponDiscount)}</td><td></td></tr>
              <tr><td>Total</td><td className="num" colSpan={4}>{money(preview.total)}</td><td></td></tr>
            </tfoot>
          </table>
          {preview.couponMessage && <p className="muted">{preview.couponMessage}</p>}
          <div className="row">
            <button type="button" onClick={handlePay}>Pay {money(preview.total)}</button>
          </div>
        </div>
      )}
    </>
  )
}
