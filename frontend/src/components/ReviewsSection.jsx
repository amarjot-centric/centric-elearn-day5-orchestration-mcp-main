import { useEffect, useState } from 'react'
import { addReview, getReviews, getSavedName, saveName, stars } from '../api.js'

export default function ReviewsSection({ courseId, onChanged }) {
  const [reviews, setReviews] = useState([])
  const [name, setName] = useState(getSavedName())
  const [starCount, setStarCount] = useState(5)
  const [comment, setComment] = useState('')
  const [message, setMessage] = useState(null)

  const load = () => getReviews(courseId).then(setReviews).catch(() => {})

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [courseId])

  async function handleSubmit(e) {
    e.preventDefault()
    try {
      await addReview(courseId, name, Number(starCount), comment)
      saveName(name.trim())
      setComment('')
      setMessage({ type: 'ok', text: 'Thanks for your review.' })
      load()
      if (onChanged) onChanged()
    } catch (err) {
      setMessage({ type: 'error', text: err.message })
    }
  }

  return (
    <section>
      <h2>Reviews</h2>
      <form className="panel form-stack" onSubmit={handleSubmit}>
        <div className="row">
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
          <select value={starCount} onChange={(e) => setStarCount(e.target.value)} aria-label="Stars">
            {[5, 4, 3, 2, 1].map((n) => <option key={n} value={n}>{n} stars</option>)}
          </select>
        </div>
        <textarea rows={2} value={comment} onChange={(e) => setComment(e.target.value)} placeholder="What did you think?" />
        <button type="submit">Post review</button>
        {message && <p className={`message ${message.type}`}>{message.text}</p>}
      </form>
      {reviews.length === 0 && <p className="muted">No reviews yet.</p>}
      {reviews.map((r) => (
        <div key={r.id} className="review">
          <span className="stars">{stars(r.stars)}</span> <strong>{r.studentName}</strong>
          <span className="muted"> · {new Date(r.createdAt).toLocaleDateString()}</span>
          {r.comment && <div>{r.comment}</div>}
        </div>
      ))}
    </section>
  )
}
