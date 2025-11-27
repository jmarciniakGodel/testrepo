import { useState, useRef } from 'react'
import axios from 'axios'
import { toast } from 'react-toastify'

export default function FileUpload() {
  const [files, setFiles] = useState([])
  const [isUploading, setIsUploading] = useState(false)
  const [isDragging, setIsDragging] = useState(false)
  const fileInputRef = useRef(null)

  // Handle file selection from input
  const handleFileSelect = (event) => {
    const selectedFiles = Array.from(event.target.files)
    addFiles(selectedFiles)
  }

  // Add files to the list, filtering for CSV only
  const addFiles = (newFiles) => {
    const csvFiles = newFiles.filter(file => 
      file.name.toLowerCase().endsWith('.csv') || file.type === 'text/csv'
    )
    
    if (csvFiles.length !== newFiles.length) {
      toast.warning('Only CSV files are allowed')
    }

    if (csvFiles.length > 0) {
      setFiles(prevFiles => {
        // Prevent duplicate files by name
        const existingNames = new Set(prevFiles.map(f => f.name))
        const uniqueFiles = csvFiles.filter(f => !existingNames.has(f.name))
        
        if (uniqueFiles.length !== csvFiles.length) {
          toast.info('Some files were already added')
        }
        
        return [...prevFiles, ...uniqueFiles]
      })
    }
  }

  // Remove file from list
  const handleRemoveFile = (index) => {
    setFiles(prevFiles => prevFiles.filter((_, i) => i !== index))
  }

  // Handle drag over
  const handleDragOver = (event) => {
    event.preventDefault()
    event.stopPropagation()
    setIsDragging(true)
  }

  // Handle drag leave
  const handleDragLeave = (event) => {
    event.preventDefault()
    event.stopPropagation()
    setIsDragging(false)
  }

  // Handle drop
  const handleDrop = (event) => {
    event.preventDefault()
    event.stopPropagation()
    setIsDragging(false)

    const droppedFiles = Array.from(event.dataTransfer.files)
    addFiles(droppedFiles)
  }

  // Handle file upload
  const handleUpload = async () => {
    if (files.length === 0) {
      toast.error('Please select at least one CSV file')
      return
    }

    setIsUploading(true)

    try {
      // Create FormData and append all files
      const formData = new FormData()
      files.forEach((file) => {
        formData.append('files', file)
      })

      // Send POST request
      const response = await axios.post('/api/home/attendancesummary', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        },
        responseType: 'blob' // Important for file download
      })

      // Handle blob response - download file
      const blob = new Blob([response.data])
      const downloadUrl = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = downloadUrl
      
      // Try to get filename from Content-Disposition header
      // Headers are case-insensitive, so we need to handle different casings
      const contentDisposition = response.headers['content-disposition'] || response.headers['Content-Disposition']
      let filename = 'attendance-summary.xlsx'
      if (contentDisposition) {
        // Match filename with or without quotes, stopping at semicolon or end of string
        const filenameMatch = contentDisposition.match(/filename[*]?=['"]?([^'";]+)['"]?/i)
        if (filenameMatch && filenameMatch[1]) {
          filename = filenameMatch[1].trim()
        }
      }
      
      link.download = filename
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(downloadUrl)

      toast.success('Files uploaded successfully! Download started.')
      
      // Clear files after successful upload
      setFiles([])
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    } catch (error) {
      console.error('Upload error:', error)
      
      // Handle error with appropriate message
      let errorMessage = 'Failed to upload files'
      
      if (error.response) {
        // Server responded with error
        if (error.response.data instanceof Blob) {
          // If error response is blob, try to read it as text
          try {
            const text = await error.response.data.text()
            errorMessage = `Upload failed: ${text}`
          } catch {
            errorMessage = `Upload failed with status ${error.response.status}`
          }
        } else if (error.response.data?.message) {
          errorMessage = error.response.data.message
        } else {
          errorMessage = `Upload failed with status ${error.response.status}`
        }
      } else if (error.request) {
        // Request made but no response
        errorMessage = 'No response from server. Please check your connection.'
      } else {
        // Something else happened
        errorMessage = error.message || 'An unexpected error occurred'
      }
      
      toast.error(errorMessage)
    } finally {
      setIsUploading(false)
    }
  }

  // Format file size
  const formatFileSize = (bytes) => {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
  }

  return (
    <div className="container mt-5">
      <div className="row">
        <div className="col-lg-8 mx-auto">
          <h1 className="text-primary mb-4">Teams Meeting Attendance Upload</h1>
          <p className="lead mb-4">
            Upload multiple CSV files from Microsoft Teams meeting attendance reports
          </p>

          {/* Drag and Drop Area */}
          <div
            className={`border rounded p-5 text-center mb-4 ${
              isDragging ? 'border-primary bg-light' : 'border-secondary'
            }`}
            style={{
              borderStyle: 'dashed',
              borderWidth: '2px',
              cursor: 'pointer',
              transition: 'all 0.3s ease'
            }}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
          >
            <div className="mb-3">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="64"
                height="64"
                fill="currentColor"
                className="text-secondary"
                viewBox="0 0 16 16"
              >
                <path d="M.5 9.9a.5.5 0 0 1 .5.5v2.5a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-2.5a.5.5 0 0 1 1 0v2.5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2v-2.5a.5.5 0 0 1 .5-.5z"/>
                <path d="M7.646 1.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1-.708.708L8.5 2.707V11.5a.5.5 0 0 1-1 0V2.707L5.354 4.854a.5.5 0 1 1-.708-.708l3-3z"/>
              </svg>
            </div>
            <h5>{isDragging ? 'Drop files here' : 'Drag & drop CSV files here'}</h5>
            <p className="text-muted mb-3">or</p>
            <button
              type="button"
              className="btn btn-outline-primary"
              onClick={(e) => {
                e.stopPropagation()
                fileInputRef.current?.click()
              }}
            >
              Browse Files
            </button>
            <input
              ref={fileInputRef}
              type="file"
              multiple
              accept=".csv,text/csv"
              onChange={handleFileSelect}
              style={{ display: 'none' }}
            />
          </div>

          {/* File List */}
          {files.length > 0 && (
            <div className="card mb-4">
              <div className="card-header d-flex justify-content-between align-items-center">
                <h5 className="mb-0">Selected Files ({files.length})</h5>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger"
                  onClick={() => setFiles([])}
                  disabled={isUploading}
                >
                  Clear All
                </button>
              </div>
              <ul className="list-group list-group-flush">
                {files.map((file, index) => (
                  <li
                    key={index}
                    className="list-group-item d-flex justify-content-between align-items-center"
                  >
                    <div className="d-flex align-items-center flex-grow-1">
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="24"
                        height="24"
                        fill="currentColor"
                        className="text-success me-2"
                        viewBox="0 0 16 16"
                      >
                        <path d="M5.5 7a.5.5 0 0 0 0 1h5a.5.5 0 0 0 0-1h-5zM5 9.5a.5.5 0 0 1 .5-.5h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1-.5-.5zm0 2a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 0 1h-2a.5.5 0 0 1-.5-.5z"/>
                        <path d="M9.5 0H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V4.5L9.5 0zm0 1v2A1.5 1.5 0 0 0 11 4.5h2V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h5.5z"/>
                      </svg>
                      <div>
                        <div className="fw-medium">{file.name}</div>
                        <small className="text-muted">{formatFileSize(file.size)}</small>
                      </div>
                    </div>
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-danger"
                      onClick={() => handleRemoveFile(index)}
                      disabled={isUploading}
                      aria-label={`Remove ${file.name}`}
                    >
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="16"
                        height="16"
                        fill="currentColor"
                        viewBox="0 0 16 16"
                      >
                        <path d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z"/>
                      </svg>
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Upload Button */}
          <div className="d-grid">
            <button
              type="button"
              className="btn btn-primary btn-lg"
              onClick={handleUpload}
              disabled={isUploading || files.length === 0}
            >
              {isUploading ? (
                <>
                  <span
                    className="spinner-border spinner-border-sm me-2"
                    role="status"
                    aria-hidden="true"
                  ></span>
                  Uploading...
                </>
              ) : (
                <>Upload {files.length > 0 ? `${files.length} file${files.length > 1 ? 's' : ''}` : 'Files'}</>
              )}
            </button>
          </div>

          {/* Info section */}
          <div className="alert alert-info mt-4" role="alert">
            <h6 className="alert-heading">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="20"
                height="20"
                fill="currentColor"
                className="me-2"
                viewBox="0 0 16 16"
              >
                <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
                <path d="m8.93 6.588-2.29.287-.082.38.45.083c.294.07.352.176.288.469l-.738 3.468c-.194.897.105 1.319.808 1.319.545 0 1.178-.252 1.465-.598l.088-.416c-.2.176-.492.246-.686.246-.275 0-.375-.193-.304-.533L8.93 6.588zM9 4.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0z"/>
              </svg>
              Instructions
            </h6>
            <ul className="mb-0 small">
              <li>Only CSV files from Microsoft Teams attendance reports are accepted</li>
              <li>You can add multiple files by dragging and dropping or using the file browser</li>
              <li>Remove individual files using the X button or clear all at once</li>
              <li>Upon successful upload, a summary report will be downloaded automatically</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  )
}
