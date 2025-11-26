# testrepo

A monolithic ASP.NET Core 8 + React + Vite application.

## Overview

This repository contains a monolithic solution with:
- **Backend**: ASP.NET Core 8 Web API at `src/Monolith.Api`
- **Frontend**: React + Vite application at `src/ClientApp`

The backend is configured to automatically start the frontend dev server when running in Development mode.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Node.js](https://nodejs.org/) (v18 or later)
- npm (comes with Node.js)

## Running the Application

### Option 1: Run Everything Together (Recommended)

The simplest way to run the application is to start the backend, which will automatically launch the frontend dev server:

```bash
dotnet run --project src/Monolith.Api
```

Or using the SPA profile with Visual Studio:
- Open `src/Monolith.sln` in Visual Studio
- Select the "SPA" launch profile
- Press F5 or click the Run button

This will:
1. Start the ASP.NET Core API on http://localhost:5163
2. Automatically spawn `npm run dev` for the React app on http://localhost:5173
3. Proxy all non-API requests to the Vite dev server

The API endpoints are available at `/api/*` (e.g., http://localhost:5163/api/weatherforecast)

### Option 2: Run Frontend Separately

If you want to run the frontend independently:

```bash
cd src/ClientApp
npm install
npm run dev
```

Then start the backend separately:

```bash
dotnet run --project src/Monolith.Api
```

## Project Structure

```
src/
├── Monolith.sln           # Solution file
├── Monolith.Api/          # ASP.NET Core 8 Web API
│   ├── Program.cs         # API startup and SPA proxy configuration
│   ├── Properties/
│   │   └── launchSettings.json  # Launch profiles including SPA profile
│   └── ...
└── ClientApp/             # React + Vite frontend
    ├── package.json       # Node dependencies and scripts
    ├── vite.config.js     # Vite configuration
    ├── src/               # React source files
    └── ...
```

## Building for Production

To build the frontend for production:

```bash
cd src/ClientApp
npm run build
```

This creates optimized static files in `src/ClientApp/dist` which the backend will serve in production mode.

To run the backend in production mode:

```bash
dotnet run --project src/Monolith.Api --configuration Release
```

## Development Notes

- The backend API listens on port 5163
- The frontend Vite dev server runs on port 5173
- API routes are prefixed with `/api/`
- In development, the backend proxies all non-API requests to the Vite dev server
- In production, the backend serves the built static files from `ClientApp/dist`

## API Documentation

When running in Development mode, Swagger UI is available at:
- http://localhost:5163/swagger
