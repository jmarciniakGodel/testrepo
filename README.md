# React + ASP.NET Core Starter (Vite + JS, Bootstrap, Axios)

This template provides:
- ASP.NET Core Web API with an empty controller
- React (Vite, JavaScript)
- Bootstrap 5 and Axios preinstalled
- Local dev configs for running both server and client

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

3. Run the client (in a new terminal):
   ```bash
   cd src/Client
   npm run dev
   ```
   Client runs on `http://localhost:5173`.

## Notes
- Update `vite.config.js` proxy as needed for your API base URL.
- Bootstrap is added via npm and imported in `main.jsx`.
- Axios is preinstalled for API calls.