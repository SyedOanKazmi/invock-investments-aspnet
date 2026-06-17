# Invock Investments (ASP.NET version)

Stock analysis & prediction platform for the Pakistan Stock Exchange.

- **Backend:** ASP.NET Core Web API (C#) with **JWT** auth, EF Core + SQLite
- **Frontend:** Vue 3 (Vite, Pinia, Chart.js) — responsive / mobile-friendly
- **Predictions:** the model stays in **Python** (Random Forest). The ASP.NET API
  runs `ml/predict.py` and serves its output.

## Project layout

```
api/   ASP.NET Core Web API  (Program.cs, Data.cs)
web/   Vue 3 frontend
ml/    Python prediction model (predict.py + data)
```

## Run locally

You need **.NET SDK**, **Node.js**, and **Python** (with the ML packages).

```bash
# one-time: install Python ML packages
pip install -r ml/requirements.txt

# 1) Backend  (http://127.0.0.1:8002)
cd api
dotnet run --urls http://127.0.0.1:8002

# 2) Frontend (http://localhost:5173)  — in a second terminal
cd web
npm install
npm run dev
```

Then open **http://localhost:5173**.

## Demo accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@psx.com | admin123 |
| Expert | expert@psx.com | expert123 |
| Investor | user@psx.com | user123 |
