# ── Stage 1: build the Vue frontend ──────────────────────────────────────────
FROM node:20-slim AS web
WORKDIR /web
COPY web/package*.json ./
RUN npm ci
COPY web/ ./
ENV VITE_API_URL=""
RUN npm run build

# ── Stage 2: publish the ASP.NET backend ─────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api/ ./api/
RUN dotnet publish api/api.csproj -c Release -o /app/publish

# ── Stage 3: runtime (.NET + Python + built site) ────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Python is needed for the prediction model.
RUN apt-get update && apt-get install -y --no-install-recommends python3 python3-pip \
    && rm -rf /var/lib/apt/lists/*
COPY ml/requirements.txt /app/ml/requirements.txt
RUN pip3 install --break-system-packages --no-cache-dir -r /app/ml/requirements.txt
COPY ml/ /app/ml/

# Published API + the built frontend (served from wwwroot).
COPY --from=build /app/publish ./
COPY --from=web /web/dist ./wwwroot

ENV ML_DIR=/app/ml
ENV PYTHON_BIN=python3
ENV ASPNETCORE_URLS=http://0.0.0.0:7860
EXPOSE 7860
CMD ["dotnet", "api.dll"]
