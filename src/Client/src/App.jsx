import { useEffect, useState } from 'react'
import axios from 'axios'

export default function App() {
  const [message, setMessage] = useState('ASP.NET Core + React starter')
  const [apiStatus, setApiStatus] = useState('Checking API...')

  useEffect(() => {
    // Example call to your API
    axios.get('/api/home')
      .then(() => {
        setApiStatus('✓ API is responding')
      })
      .catch(() => {
        setApiStatus('✗ API is not available')
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
          <p className="text-success">{apiStatus}</p>
          <button className="btn btn-success">Bootstrap Button</button>
        </div>
      </div>
    </div>
  )
}
