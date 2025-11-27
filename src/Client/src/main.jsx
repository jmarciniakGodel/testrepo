import React from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'

// Import Bootstrap CSS and JS
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap'

// Import react-toastify CSS
import 'react-toastify/dist/ReactToastify.css'

createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
)
