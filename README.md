# CSV to Excel Converter

A full-stack application that allows users to upload multiple CSV files and generates an Excel file with a summary table and individual sheets for each CSV file.

## Tech Stack

### Frontend
- React with Vite
- JavaScript
- Axios for HTTP requests
- Bootstrap 5 for styling

### Backend
- .NET 8 Web API
- EPPlus for Excel generation
- C#

## Features

- Upload multiple CSV files simultaneously
- Generates Excel file with:
  - Summary sheet showing statistics (file name, row count, column count) for each uploaded CSV
  - Individual sheets for each CSV file with formatted data
- Responsive UI with Bootstrap styling
- File validation (CSV only)
- Download generated Excel file automatically

## Prerequisites

- Node.js 18+ and npm
- .NET 8 SDK

## Setup and Running

### Backend Setup

1. Navigate to the backend directory:
```bash
cd backend/CsvToExcelApi
```

2. Restore dependencies (automatically done on first build):
```bash
dotnet restore
```

3. Run the backend:
```bash
dotnet run --launch-profile http
```

The API will be available at `http://localhost:5000`
Swagger UI will be available at `http://localhost:5000/swagger`

### Frontend Setup

1. Navigate to the frontend directory:
```bash
cd frontend
```

2. Install dependencies:
```bash
npm install
```

3. (Optional) Create a `.env.development` file to configure the API URL:
```bash
VITE_API_URL=http://localhost:5000
```

4. Run the development server:
```bash
npm run dev
```

The frontend will be available at `http://localhost:5173`

## Usage

1. Start both the backend and frontend servers
2. Open your browser to `http://localhost:5173`
3. Click "Select CSV Files" and choose one or more CSV files
4. Click "Generate Excel Summary"
5. The Excel file will automatically download with:
   - A "Summary" sheet showing statistics for all uploaded files
   - Individual sheets for each CSV file with the full data

## API Endpoints

### POST /api/CsvToExcel/convert
Upload multiple CSV files and receive an Excel file with summary.

**Request:**
- Content-Type: multipart/form-data
- Body: Multiple files with key "files"

**Response:**
- Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
- Body: Excel file (summary.xlsx)

## Project Structure

```
├── backend/
│   └── CsvToExcelApi/
│       ├── Controllers/
│       │   ├── CsvToExcelController.cs
│       │   └── WeatherForecastController.cs
│       ├── Properties/
│       ├── Program.cs
│       └── CsvToExcelApi.csproj
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   └── CsvUploader.jsx
│   │   ├── App.jsx
│   │   ├── App.css
│   │   ├── main.jsx
│   │   └── index.css
│   ├── package.json
│   └── vite.config.js
└── README.md
```

## License

This is a sample project for demonstration purposes.
