"""
predict.py — prediction helper for the ASP.NET backend.

The ASP.NET API runs this script and uses its JSON output, e.g.:
    python predict.py stocks
    python predict.py history OGDC
    python predict.py summary OGDC
    python predict.py predict OGDC 7

The model stays in Python (Random Forest); ASP.NET just consumes the output.
Reads psx_stocks.csv (3 stocks) sitting next to this file.
"""
import sys
import json
import os
import warnings

warnings.filterwarnings("ignore")
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.metrics import mean_absolute_percentage_error

DATA = os.path.join(os.path.dirname(__file__), "psx_stocks.csv")
FEATURES = ["Open", "High", "Low", "Volume", "MA7", "MA21", "Lag1"]

STOCKS = [
    {"symbol": "OGDC", "name": "Oil & Gas Development Co.", "sector": "Energy"},
    {"symbol": "HBL",  "name": "Habib Bank",               "sector": "Banking"},
    {"symbol": "LUCK", "name": "Lucky Cement",             "sector": "Cement"},
]


def load(ticker):
    df = pd.read_csv(DATA)
    df = df[df["Ticker"] == ticker].copy()
    df["Date"] = pd.to_datetime(df["Date"], format="mixed")
    return df.sort_values("Date").reset_index(drop=True)


def add_features(df):
    df = df.copy()
    df["MA7"] = df["Close"].rolling(7).mean()
    df["MA21"] = df["Close"].rolling(21).mean()
    df["Lag1"] = df["Close"].shift(1)
    return df.dropna().reset_index(drop=True)


def train(df):
    split = int(len(df) * 0.8)
    tr, te = df.iloc[:split], df.iloc[split:]
    model = RandomForestRegressor(n_estimators=100, random_state=42)
    model.fit(tr[FEATURES], tr["Close"])
    pred = model.predict(te[FEATURES])
    acc = round((1 - mean_absolute_percentage_error(te["Close"], pred)) * 100, 2)
    return model, te, pred, acc


def latest_quote(ticker):
    df = load(ticker)
    if len(df) < 2:
        return None, None
    last, prev = df["Close"].iloc[-1], df["Close"].iloc[-2]
    return round(last, 2), round((last - prev) / prev * 100, 2)


def cmd_stocks():
    out = []
    for s in STOCKS:
        price, change = latest_quote(s["symbol"])
        out.append({**s, "price": price, "change": change})
    return out


def cmd_history(ticker):
    df = load(ticker)
    cutoff = df["Date"].iloc[-1] - pd.Timedelta(days=365)
    df = df[df["Date"] >= cutoff]
    return {"ticker": ticker,
            "dates": df["Date"].dt.strftime("%Y-%m-%d").tolist(),
            "close": df["Close"].tolist(),
            "volume": df["Volume"].tolist()}


def cmd_summary(ticker):
    df = load(ticker)
    latest, prev = df.iloc[-1], df.iloc[-2]
    change = round(latest["Close"] - prev["Close"], 2)
    return {"ticker": ticker,
            "latest_close": round(latest["Close"], 2),
            "change": change,
            "pct_change": round(change / prev["Close"] * 100, 2),
            "high_52w": round(df.tail(252)["High"].max(), 2),
            "low_52w": round(df.tail(252)["Low"].min(), 2),
            "avg_volume": int(df.tail(30)["Volume"].mean()),
            "last_date": latest["Date"].strftime("%Y-%m-%d")}


def cmd_predict(ticker, days):
    df = add_features(load(ticker))
    model, te, pred, acc = train(df)
    last = df.iloc[-1].copy()
    date = df["Date"].iloc[-1]
    future = []
    for _ in range(days):
        date = date + pd.tseries.offsets.BusinessDay(1)  # skip weekends
        row = pd.DataFrame([last[FEATURES].values], columns=FEATURES)
        price = round(float(model.predict(row)[0]), 2)
        future.append({"date": date.strftime("%Y-%m-%d"), "price": price})
        last["Lag1"] = last["Close"] = last["Open"] = price
    te2 = te.tail(60)
    return {"ticker": ticker, "accuracy": acc, "future": future,
            "test_dates": te2["Date"].dt.strftime("%Y-%m-%d").tolist(),
            "test_actual": [round(v, 2) for v in te2["Close"].tolist()],
            "test_predicted": [round(float(v), 2) for v in pred[-60:]]}


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "stocks":
        print(json.dumps(cmd_stocks()))
    elif cmd == "history":
        print(json.dumps(cmd_history(sys.argv[2])))
    elif cmd == "summary":
        print(json.dumps(cmd_summary(sys.argv[2])))
    elif cmd == "predict":
        print(json.dumps(cmd_predict(sys.argv[2], int(sys.argv[3]))))
