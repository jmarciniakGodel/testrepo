import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom'
import { ToastContainer } from 'react-toastify'
import FileUpload from './FileUpload'
import Summaries from './Summaries'

/**
 * App component - Main application component with routing and navigation
 * @returns {JSX.Element} The rendered application
 */
export default function App() {
  return (
    <Router>
      <>
        <nav className="navbar navbar-expand-lg navbar-light bg-light shadow-sm">
          <div className="container">
            <Link className="navbar-brand fw-bold text-primary" to="/">
              Meeting Attendance Manager
            </Link>
            <div className="d-flex">
              <Link to="/summaries" className="btn btn-outline-primary">
                View All Summaries
              </Link>
            </div>
          </div>
        </nav>
        
        <Routes>
          <Route path="/" element={<FileUpload />} />
          <Route path="/summaries" element={<Summaries />} />
        </Routes>
        
        <ToastContainer
          position="top-right"
          autoClose={5000}
          hideProgressBar={false}
          newestOnTop={false}
          closeOnClick
          rtl={false}
          pauseOnFocusLoss
          draggable
          pauseOnHover
          theme="light"
        />
      </>
    </Router>
  )
}
