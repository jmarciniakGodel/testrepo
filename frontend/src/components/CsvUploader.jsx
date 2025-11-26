import { useState } from 'react';
import axios from 'axios';

function CsvUploader() {
  const [files, setFiles] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleFileChange = (e) => {
    const selectedFiles = Array.from(e.target.files);
    const csvFiles = selectedFiles.filter(file => 
      file.name.toLowerCase().endsWith('.csv')
    );

    if (csvFiles.length !== selectedFiles.length) {
      setError('Please select only CSV files');
      return;
    }

    setFiles(csvFiles);
    setError('');
    setSuccess('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (files.length === 0) {
      setError('Please select at least one CSV file');
      return;
    }

    setLoading(true);
    setError('');
    setSuccess('');

    try {
      const formData = new FormData();
      files.forEach(file => {
        formData.append('files', file);
      });

      const response = await axios.post(
        'http://localhost:5000/api/CsvToExcel/convert',
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
          responseType: 'blob',
        }
      );

      // Create download link
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'summary.xlsx');
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);

      setSuccess('Excel file generated successfully!');
      setFiles([]);
      e.target.reset();
    } catch (err) {
      console.error('Error:', err);
      setError(err.response?.data?.message || 'Error processing files. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const removeFile = (index) => {
    setFiles(files.filter((_, i) => i !== index));
  };

  return (
    <div className="container py-5">
      <div className="row justify-content-center">
        <div className="col-lg-8">
          <div className="card shadow">
            <div className="card-body p-5">
              <h1 className="card-title text-center mb-4">CSV to Excel Converter</h1>
              <p className="text-center text-muted mb-4">
                Upload multiple CSV files to generate an Excel summary
              </p>

              <form onSubmit={handleSubmit}>
                <div className="mb-4">
                  <label htmlFor="csvFiles" className="form-label fw-bold">
                    Select CSV Files
                  </label>
                  <input
                    type="file"
                    className="form-control form-control-lg"
                    id="csvFiles"
                    accept=".csv"
                    multiple
                    onChange={handleFileChange}
                    disabled={loading}
                  />
                  <div className="form-text">
                    You can select multiple CSV files at once
                  </div>
                </div>

                {files.length > 0 && (
                  <div className="mb-4">
                    <h6 className="fw-bold">Selected Files ({files.length}):</h6>
                    <ul className="list-group">
                      {files.map((file, index) => (
                        <li
                          key={index}
                          className="list-group-item d-flex justify-content-between align-items-center"
                        >
                          <span>
                            <i className="bi bi-file-earmark-spreadsheet me-2"></i>
                            {file.name}
                          </span>
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => removeFile(index)}
                            disabled={loading}
                          >
                            Remove
                          </button>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {error && (
                  <div className="alert alert-danger" role="alert">
                    {error}
                  </div>
                )}

                {success && (
                  <div className="alert alert-success" role="alert">
                    {success}
                  </div>
                )}

                <div className="d-grid">
                  <button
                    type="submit"
                    className="btn btn-primary btn-lg"
                    disabled={loading || files.length === 0}
                  >
                    {loading ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                        Processing...
                      </>
                    ) : (
                      'Generate Excel Summary'
                    )}
                  </button>
                </div>
              </form>
            </div>
          </div>

          <div className="mt-4 text-center text-muted">
            <small>
              The generated Excel file will contain a summary sheet with file statistics 
              and individual sheets for each CSV file uploaded.
            </small>
          </div>
        </div>
      </div>
    </div>
  );
}

export default CsvUploader;
