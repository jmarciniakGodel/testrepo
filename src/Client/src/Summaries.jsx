import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import axios from 'axios'
import { toast } from 'react-toastify'

/**
 * Summaries component - Displays a list of meeting summaries with pagination and search
 * @returns {JSX.Element} The rendered summaries page
 */
export default function Summaries() {
  const [summaries, setSummaries] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [expandedId, setExpandedId] = useState(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize] = useState(5)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [searchQuery, setSearchQuery] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [summaryNumber, setSummaryNumber] = useState('')
  const [summaryNumberInput, setSummaryNumberInput] = useState('')
  const [sortDescending, setSortDescending] = useState(true)

  /**
   * Fetches summaries from the API with pagination and search parameters
   */
  const fetchSummaries = async () => {
    try {
      setIsLoading(true)
      const response = await axios.get('/api/MeetingUpload', {
        params: {
          page: currentPage,
          pageSize: pageSize,
          search: searchQuery || undefined,
          number: summaryNumber || undefined,
          sortDesc: sortDescending
        }
      })
      setSummaries(response.data.summaries)
      setTotalPages(response.data.totalPages)
      setTotalCount(response.data.totalCount)
    } catch (error) {
      console.error('Error fetching summaries:', error)
      toast.error('Failed to load summaries')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    fetchSummaries()
  }, [currentPage, searchQuery, summaryNumber, sortDescending])

  /**
   * Handles the Excel file download for a specific summary
   * @param {number} summaryId - The ID of the summary to download
   */
  const handleDownload = async (summaryId) => {
    try {
      const response = await axios.get(`/api/MeetingUpload/${summaryId}/download`, {
        responseType: 'blob'
      })

      const blob = new Blob([response.data])
      const downloadUrl = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = downloadUrl
      
      const contentDisposition = response.headers['content-disposition'] || response.headers['Content-Disposition']
      let filename = 'meeting-summary.xlsx'
      if (contentDisposition) {
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

      toast.success('Excel file downloaded successfully!')
    } catch (error) {
      console.error('Download error:', error)
      toast.error('Failed to download Excel file')
    }
  }

  /**
   * Toggles the expanded state of a summary item
   * @param {number} id - The ID of the summary to toggle
   */
  const toggleExpand = (id) => {
    setExpandedId(expandedId === id ? null : id)
  }

  /**
   * Formats a date string to a locale-specific format
   * @param {string} dateString - The date string to format
   * @returns {string} The formatted date string
   */
  const formatDate = (dateString) => {
    const date = new Date(dateString)
    return date.toLocaleString()
  }

  /**
   * Handles search form submission
   * @param {Event} e - The form submission event
   */
  const handleSearch = (e) => {
    e.preventDefault()
    setSearchQuery(searchInput)
    setSummaryNumber(summaryNumberInput)
    setCurrentPage(1) // Reset to first page on new search
  }

  /**
   * Clears all search filters
   */
  const handleClearSearch = () => {
    setSearchInput('')
    setSearchQuery('')
    setSummaryNumberInput('')
    setSummaryNumber('')
    setCurrentPage(1)
  }

  /**
   * Toggles the sort order for created date
   */
  const toggleSort = () => {
    setSortDescending(!sortDescending)
    setCurrentPage(1)
  }

  /**
   * Navigates to a specific page
   * @param {number} page - The page number to navigate to
   */
  const goToPage = (page) => {
    if (page >= 1 && page <= totalPages) {
      setCurrentPage(page)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }

  if (isLoading) {
    return (
      <div className="container mt-5">
        <div className="text-center">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
          <p className="mt-3">Loading summaries...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="container mt-5">
      <div className="row">
        <div className="col-lg-10 mx-auto">
          <div className="d-flex justify-content-between align-items-center mb-4">
            <h1 className="text-primary mb-0">Meeting Summaries</h1>
            <Link to="/" className="btn btn-outline-primary">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="16"
                height="16"
                fill="currentColor"
                className="me-2"
                viewBox="0 0 16 16"
              >
                <path fillRule="evenodd" d="M15 8a.5.5 0 0 0-.5-.5H2.707l3.147-3.146a.5.5 0 1 0-.708-.708l-4 4a.5.5 0 0 0 0 .708l4 4a.5.5 0 0 0 .708-.708L2.707 8.5H14.5A.5.5 0 0 0 15 8z"/>
              </svg>
              Back to Upload
            </Link>
          </div>

          {/* Search Bar */}
          <div className="card mb-4">
            <div className="card-body">
              <form onSubmit={handleSearch}>
                <div className="row g-2 mb-2">
                  <div className="col-md-4">
                    <label htmlFor="searchInput" className="form-label small">Search by meeting title</label>
                    <input
                      id="searchInput"
                      type="text"
                      className="form-control"
                      placeholder="e.g., Kowalski..."
                      value={searchInput}
                      onChange={(e) => setSearchInput(e.target.value)}
                    />
                  </div>
                  <div className="col-md-3">
                    <label htmlFor="numberInput" className="form-label small">Filter by number</label>
                    <input
                      id="numberInput"
                      type="number"
                      className="form-control"
                      placeholder="Summary number..."
                      value={summaryNumberInput}
                      onChange={(e) => setSummaryNumberInput(e.target.value)}
                      min="1"
                    />
                  </div>
                  <div className="col-md-3 d-flex align-items-end">
                    <button type="submit" className="btn btn-primary me-2">
                      Search
                    </button>
                    {(searchQuery || summaryNumber) && (
                      <button 
                        type="button" 
                        className="btn btn-outline-secondary"
                        onClick={handleClearSearch}
                      >
                        Clear
                      </button>
                    )}
                  </div>
                  <div className="col-md-2 d-flex align-items-end">
                    <button 
                      type="button" 
                      className="btn btn-outline-primary w-100"
                      onClick={toggleSort}
                      title={sortDescending ? "Click to sort oldest first" : "Click to sort newest first"}
                    >
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        width="16"
                        height="16"
                        fill="currentColor"
                        className="me-1"
                        viewBox="0 0 16 16"
                      >
                        {sortDescending ? (
                          <path d="M3.5 2.5a.5.5 0 0 0-1 0v8.793l-1.146-1.147a.5.5 0 0 0-.708.708l2 1.999.007.007a.497.497 0 0 0 .7-.006l2-2a.5.5 0 0 0-.707-.708L3.5 11.293V2.5zm3.5 1a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zM7.5 6a.5.5 0 0 0 0 1h5a.5.5 0 0 0 0-1h-5zm0 3a.5.5 0 0 0 0 1h3a.5.5 0 0 0 0-1h-3zm0 3a.5.5 0 0 0 0 1h1a.5.5 0 0 0 0-1h-1z"/>
                        ) : (
                          <path d="M3.5 12.5a.5.5 0 0 1-1 0V3.707L1.354 4.854a.5.5 0 1 1-.708-.708l2-1.999.007-.007a.497.497 0 0 1 .7.006l2 2a.5.5 0 1 1-.707.708L3.5 3.707V12.5zm3.5-9a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zM7.5 6a.5.5 0 0 0 0 1h5a.5.5 0 0 0 0-1h-5zm0 3a.5.5 0 0 0 0 1h3a.5.5 0 0 0 0-1h-3zm0 3a.5.5 0 0 0 0 1h1a.5.5 0 0 0 0-1h-1z"/>
                        )}
                      </svg>
                      {sortDescending ? 'Newest' : 'Oldest'}
                    </button>
                  </div>
                </div>
              </form>
              {(searchQuery || summaryNumber) && (
                <small className="text-muted mt-2 d-block">
                  {searchQuery && <span>Searching for: <strong>{searchQuery}</strong></span>}
                  {searchQuery && summaryNumber && <span> | </span>}
                  {summaryNumber && <span>Number: <strong>{summaryNumber}</strong></span>}
                </small>
              )}
            </div>
          </div>

          {summaries.length === 0 ? (
            <div className="alert alert-info">
              <h5>No Summaries Found</h5>
              <p className="mb-0">
                {searchQuery 
                  ? 'No summaries match your search criteria. Try a different search term.'
                  : 'Upload some CSV files to see summaries here.'}
              </p>
            </div>
          ) : (
            <>
              <div className="card">
                <div className="card-header">
                  <h5 className="mb-0">
                    All Summaries ({totalCount} total, showing page {currentPage} of {totalPages})
                  </h5>
                </div>
                <div className="list-group list-group-flush">
                  {summaries.map((summary) => (
                    <div key={summary.id} className="list-group-item">
                      <div className="d-flex justify-content-between align-items-start">
                        <div className="flex-grow-1">
                          <div className="d-flex align-items-center mb-2">
                            <h6 className="mb-0 me-3">Summary #{summary.id}</h6>
                            <span className="badge bg-secondary">{summary.meetingCount} meeting{summary.meetingCount !== 1 ? 's' : ''}</span>
                          </div>
                          <p className="text-muted small mb-2">
                            Created: {formatDate(summary.createdAt)}
                          </p>
                          <div className="btn-group btn-group-sm">
                            <button
                              className="btn btn-outline-primary"
                              onClick={() => toggleExpand(summary.id)}
                            >
                              {expandedId === summary.id ? 'Hide' : 'Show'} Details
                            </button>
                            <button
                              className="btn btn-success"
                              onClick={() => handleDownload(summary.id)}
                            >
                              <svg
                                xmlns="http://www.w3.org/2000/svg"
                                width="16"
                                height="16"
                                fill="currentColor"
                                className="me-1"
                                viewBox="0 0 16 16"
                              >
                                <path d="M.5 9.9a.5.5 0 0 1 .5.5v2.5a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-2.5a.5.5 0 0 1 1 0v2.5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2v-2.5a.5.5 0 0 1 .5-.5z"/>
                                <path d="M7.646 11.854a.5.5 0 0 0 .708 0l3-3a.5.5 0 0 0-.708-.708L8.5 10.293V1.5a.5.5 0 0 0-1 0v8.793L5.354 8.146a.5.5 0 1 0-.708.708l3 3z"/>
                              </svg>
                              Download Excel
                            </button>
                          </div>
                        </div>
                      </div>
                      
                      {expandedId === summary.id && summary.htmlTable && (
                        <div className="mt-3 border-top pt-3">
                          <h6 className="text-secondary mb-2">Summary Details</h6>
                          <div 
                            className="table-responsive"
                            dangerouslySetInnerHTML={{ __html: summary.htmlTable }}
                          />
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>

              {/* Pagination Controls */}
              {totalPages > 1 && (
                <nav aria-label="Summary pagination" className="mt-4">
                  <ul className="pagination justify-content-center">
                    <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                      <button 
                        className="page-link" 
                        onClick={() => goToPage(currentPage - 1)}
                        disabled={currentPage === 1}
                      >
                        Previous
                      </button>
                    </li>
                    
                    {[...Array(totalPages)].map((_, index) => {
                      const page = index + 1
                      // Show first page, last page, current page, and pages around current
                      if (
                        page === 1 || 
                        page === totalPages || 
                        (page >= currentPage - 1 && page <= currentPage + 1)
                      ) {
                        return (
                          <li key={page} className={`page-item ${currentPage === page ? 'active' : ''}`}>
                            <button 
                              className="page-link" 
                              onClick={() => goToPage(page)}
                            >
                              {page}
                            </button>
                          </li>
                        )
                      } else if (page === currentPage - 2 || page === currentPage + 2) {
                        return <li key={page} className="page-item disabled"><span className="page-link">...</span></li>
                      }
                      return null
                    })}
                    
                    <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
                      <button 
                        className="page-link" 
                        onClick={() => goToPage(currentPage + 1)}
                        disabled={currentPage === totalPages}
                      >
                        Next
                      </button>
                    </li>
                  </ul>
                </nav>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}
