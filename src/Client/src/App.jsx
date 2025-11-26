import { useEffect, useState } from 'react'
import axios from 'axios'

export default function App() {
  const [message, setMessage] = useState('ASP.NET Core + React starter')

  useEffect(() => {
    // Example call to your API; will 404 until you add an action in HomeController.
    axios.get('/api/home')
      .then(() => {
        // No content yet.
      })
      .catch(() => {
        // Controller is empty; ignore for now.
      })
  }, [])

  return (
    <div className="container mt-5">
      <div className="row">
        <div className="col">
          <h1 className="text-primary">{message}</h1>
          <p className="lead">
            Your backend is on <code>http://localhost:5000</code> and
            your frontend on <code>http://localhost:5173</code>.
          </p>
          <button className="btn btn-success">Bootstrap Button</button>
        </div>
      </div>
    </div>
  )
}
