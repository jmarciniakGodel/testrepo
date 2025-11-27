import { ToastContainer } from 'react-toastify'
import FileUpload from './FileUpload'

export default function App() {
  return (
    <>
      <FileUpload />
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
  )
}
