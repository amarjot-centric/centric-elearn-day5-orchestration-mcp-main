import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getQuiz, submitQuiz, getSavedName, saveName } from '../api.js'

export default function QuizPage() {
  const { lessonId } = useParams()
  const [questions, setQuestions] = useState([])
  const [answers, setAnswers] = useState({}) // questionId -> selectedIndex
  const [name, setName] = useState(getSavedName())
  const [result, setResult] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    getQuiz(lessonId).then(setQuestions).catch((e) => setError(e.message))
  }, [lessonId])

  async function handleSubmit(e) {
    e.preventDefault()
    try {
      const list = Object.entries(answers).map(([questionId, selectedIndex]) => ({
        questionId: Number(questionId),
        selectedIndex,
      }))
      setResult(await submitQuiz(lessonId, name, list))
      saveName(name.trim())
      setError('')
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <>
      <Link to="/">&larr; All courses</Link>
      <h1>Lesson quiz</h1>
      {error && <p className="message error">{error}</p>}

      <form onSubmit={handleSubmit}>
        {questions.map((q, i) => (
          <div key={q.id} className="panel">
            <strong>{i + 1}. {q.question}</strong>
            {q.options.map((option, index) => (
              <label key={index} className="option">
                <input
                  type="radio"
                  name={`q${q.id}`}
                  checked={answers[q.id] === index}
                  onChange={() => setAnswers({ ...answers, [q.id]: index })}
                />
                {option}
              </label>
            ))}
          </div>
        ))}

        {questions.length > 0 && (
          <div className="row">
            <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Your name" />
            <button type="submit">Submit answers</button>
          </div>
        )}
      </form>

      {result && (
        <p className={`message ${result.passed ? 'ok' : 'error'}`}>
          You scored {result.score} out of {result.total}. {result.passed ? 'You passed!' : 'Not passed yet. Try again.'}
        </p>
      )}
    </>
  )
}
