# Meeting Attendance Manager

A full-stack application for managing and analyzing meeting attendance data through CSV file uploads.

## Features
- Upload multiple meeting attendance CSV files
- Automatic summary generation with attendance duration tracking
- View and manage all meeting summaries
- Export summaries to Excel format
- Modern React frontend with Bootstrap 5 UI
- ASP.NET Core Web API backend with Entity Framework Core

## Structure
- `src/Server` — ASP.NET Core backend
- `src/Client` — Vite-powered React frontend

## Prerequisites
- .NET 8 SDK
- Node.js 18+

## Getting Started

1. Install client dependencies:
   ```bash
   cd src/Client
   npm install
   ```

2. Run the server:
   ```bash
   cd ../Server
   dotnet run
   ```
   Server listens on `http://localhost:5000` by default.
   Swagger documentation available at `http://localhost:5000/swagger`

3. Run the client (in a new terminal):
   ```bash
   cd src/Client
   npm run dev
   ```
   Client runs on `http://localhost:5173`.

## CSV File Format

The application supports two CSV formats:

### Microsoft Teams Attendance Report Format (Recommended)

Export attendance reports directly from Microsoft Teams. The CSV file includes:
- **1. Summary** section with meeting title, start time, end time, and duration
- **2. Participants** section with participant details including:
  - Name, First Join, Last Leave, In-Meeting Duration, Email, Participant ID (UPN), Role
- **3. In-Meeting Activities** section

Example structure:
```
1. Summary
Meeting title	Meeting with Jan Nowak
Start time	11/26/25, 4:16:54 PM
End time	11/26/25, 4:18:08 PM

2. Participants
Name	First Join	Last Leave	In-Meeting Duration	Email	Participant ID (UPN)	Role
Jan Nowak	11/26/25, 4:16:55 PM	11/26/25, 4:18:08 PM	1m 13s	j.nowak@gmail.com	j.nowak@gmail.com	Organizer
```

**Note:** The application automatically detects and parses Teams format with tab-separated values.

### Simple CSV Format (Alternative)

For custom data, use this simplified format:
```csv
Meeting Title,2024-01-15
Name,Email,Duration
John Doe,john@example.com,45
Jane Smith,jane@example.com,30
```

- **Line 1**: Meeting title and date (comma-separated)
- **Line 2**: Column headers (Name, Email, Duration)
- **Line 3+**: Attendee data rows
- **Duration**: Can be specified as plain minutes (e.g., `45`) or with unit (e.g., `45 min`, `45 minutes`)

## API Documentation

Comprehensive API documentation is available in `src/Server/API_DOCUMENTATION.md`

Key endpoints:
- `POST /api/MeetingUpload` - Upload CSV files and generate summary
- `GET /api/MeetingUpload` - Retrieve all summaries
- `GET /api/MeetingUpload/{id}` - Retrieve specific summary
- `GET /api/MeetingUpload/{id}/download` - Download Excel summary

## Technologies Used

**Backend:**
- ASP.NET Core 8.0 Web API
- Entity Framework Core 8.0 with SQLite
- CsvHelper for CSV parsing
- ClosedXML for Excel generation

**Frontend:**
- React 18 with Vite
- React Router for navigation
- Bootstrap 5 for styling
- Axios for API calls
- React Toastify for notifications

## Testing

Run the server-side tests:
```bash
cd tests/Server.Tests
dotnet test
```

The test suite includes 21 comprehensive tests covering file validation, CSV parsing, HTML/XLSX generation, and API endpoints.

## Notes
- The API proxy is configured in `vite.config.js` to forward requests to the backend
- All attendees are uniquely identified by email address
- The application automatically handles duplicate attendees across multiple meetings
- Summaries include formatted attendance durations (hours and minutes)