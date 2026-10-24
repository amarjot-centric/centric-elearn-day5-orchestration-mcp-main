import { useEffect, useState } from 'react'
import { getCertificates, getSavedName, saveName } from '../api.js'

export default function CertificatesPage() {
  const [name, setName] = useState(getSavedName())
  const [certs, setCerts] = useState([])
  const [searched, setSearched] = useState(false)
  const [error, setError] = useState('')

  async function load(studentName) {
    if (!studentName.trim()) return
    try {
      setCerts(await getCertificates(studentName))
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

  return (
    <>
      <h1>Certificates</h1>
      <form className="row" onSubmit={handleSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
        <button type="submit">Show my certificates</button>
      </form>

      {error && <p className="message error">{error}</p>}
      {searched && certs.length === 0 && (
        <p className="muted">No certificates yet. Finish every lesson of a course to earn one.</p>
      )}
      <div className="certificates">
        {certs.map((c) => (
          <div key={c.certificateId} className="certificate">
            <div className="certificate-inner">
              <p className="certificate-brand">LearnHub</p>
              <h2>Certificate of Completion</h2>
              <p className="muted">This certifies that</p>
              <p className="certificate-name">{c.studentName}</p>
              <p className="muted">has completed the course</p>
              <p className="certificate-course">{c.courseTitle}</p>
              <div className="certificate-foot">
                <span>Issued on {new Date(c.issuedOn).toLocaleDateString()}</span>
                <span>ID: {c.certificateId}</span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </>
  )
}
