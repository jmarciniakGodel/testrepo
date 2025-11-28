# Meeting CSV Upload API

## Overview
This API provides endpoints for uploading meeting attendance CSV files, generating summaries, and retrieving summary data.

## Swagger Documentation
Interactive API documentation is available at `/swagger/index.html` when running in Development mode.

## Endpoints

### POST `/api/MeetingUpload`
Upload multiple meeting attendance CSV files and generate a summary.

#### Request
- **Content-Type**: `multipart/form-data`
- **Body**: Multiple file uploads with field name `files`

#### CSV Format
Each CSV file should have the following structure:

```csv
Meeting Title,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45
Jane Smith,jane@example.com,30
```

- **Line 1**: Meeting title and date (comma-separated)
- **Line 2**: Column headers (Name, Email, Duration)
- **Line 3+**: Attendee data rows

#### Duration Format
Duration can be specified in minutes as:
- Plain number: `45` (interpreted as 45 minutes)
- With unit: `45 min`, `45 mins`, `45 minutes`

#### Response (Success - 200 OK)
```json
{
  "summaryId": 1,
  "htmlTable": "<table border='1'...></table>"
}
```

#### Response (Error - 400 Bad Request)
```json
{
  "error": "Error message describing the issue"
}
```

### GET `/api/MeetingUpload`
Retrieve all summaries.

#### Response (Success - 200 OK)
```json
[
  {
    "id": 1,
    "createdAt": "2024-01-15T10:30:00",
    "meetingCount": 2,
    "htmlTable": "<table>...</table>"
  }
]
```

### GET `/api/MeetingUpload/{id}`
Retrieve a specific summary by ID.

#### Parameters
- `id` (path parameter): The ID of the summary to retrieve

#### Response (Success - 200 OK)
```json
{
  "id": 1,
  "createdAt": "2024-01-15T10:30:00",
  "meetingCount": 2,
  "htmlTable": "<table>...</table>",
  "meetings": [
    {
      "id": 1,
      "title": "Team Standup",
      "date": "2024-01-15T00:00:00",
      "attendeeCount": 4
    }
  ]
}
```

#### Response (Not Found - 404)
```json
{
  "error": "Summary with ID 999 not found"
}
```

### GET `/api/MeetingUpload/{id}/download`
Download the Excel summary file for a specific summary.

#### Parameters
- `id` (path parameter): The ID of the summary to download

#### Response (Success - 200 OK)
Returns an Excel file (.xlsx) with the summary data.
- **Content-Type**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **Content-Disposition**: `attachment; filename="meeting-summary-YYYYMMDD-HHmmss.xlsx"`

#### Response (Not Found - 404)
```json
{
  "error": "Summary with ID 999 not found"
}
```

## Example Usage

### Upload CSV Files (cURL)
```bash
curl -X POST http://localhost:5000/api/MeetingUpload \
  -F "files=@meeting1.csv;type=text/csv" \
  -F "files=@meeting2.csv;type=text/csv"
```

### Get All Summaries (cURL)
```bash
curl -X GET http://localhost:5000/api/MeetingUpload
```

### Get Summary by ID (cURL)
```bash
curl -X GET http://localhost:5000/api/MeetingUpload/1
```

### JavaScript (Axios)
```javascript
// Upload files
const formData = new FormData();
formData.append('files', file1);
formData.append('files', file2);

const uploadResponse = await axios.post('/api/MeetingUpload', formData, {
  headers: { 'Content-Type': 'multipart/form-data' }
});

console.log('Summary ID:', uploadResponse.data.summaryId);

// Get all summaries
const allSummaries = await axios.get('/api/MeetingUpload');
console.log('All Summaries:', allSummaries.data);

// Get specific summary
const summary = await axios.get(`/api/MeetingUpload/${uploadResponse.data.summaryId}`);
console.log('Summary Details:', summary.data);

// Download Excel summary
const excelBlob = await axios.get(`/api/MeetingUpload/${uploadResponse.data.summaryId}/download`, {
  responseType: 'blob'
});

// Create download link
const url = window.URL.createObjectURL(new Blob([excelBlob.data]));
const link = document.createElement('a');
link.href = url;
link.setAttribute('download', 'meeting-summary.xlsx');
document.body.appendChild(link);
link.click();
link.remove();
```

## Features

### File Validation
- Validates MIME types (text/csv, application/csv, text/plain)
- Validates file content (checks for valid CSV structure)

### Data Processing
- Parses meeting title and date from first line
- Extracts attendee information (name, email, duration)
- Handles duplicate attendees across meetings
- Calculates total attendance duration per attendee per meeting

### Summary Generation
- Creates HTML table with:
  - Rows: Unique attendees (by email)
  - Columns: Meetings (title + date)
  - Cells: Attendance duration (formatted as "X hr Y min")
- Generates XLSX file with the same data structure

### Database Storage
- Stores attendants with unique email constraint
- Creates meeting records
- Records attendance with duration for each attendee per meeting
- Stores summary with HTML and XLSX data

## Technologies Used
- **ASP.NET Core 8.0**: Web API framework
- **Entity Framework Core 8.0**: ORM with SQLite database
- **CsvHelper**: CSV parsing (MIT-compatible license)
- **ClosedXML**: XLSX generation (MIT license)

## Testing
The implementation includes 21 comprehensive tests covering:
- File validation (MIME types, content validation)
- CSV parsing (valid files, multiple attendees, malformed data)
- HTML generation (valid data, empty attendance)
- XLSX generation (valid data, file format verification)
- Controller endpoints (success cases, validation errors, duplicate handling)

Run tests with:
```bash
cd tests/Server.Tests
dotnet test
```

## Security
- No vulnerabilities detected by CodeQL scanner
- Input validation on all file uploads
- Safe CSV parsing with error handling
- All dependencies use MIT or compatible licenses
