# Mobile Access Guide

## Quick Start
1. **PC (Localhost)**: Just open `http://localhost:5173`. The app will automatically connect to the backend.
2. **Mobile (Phone)**: You need an active Ngrok tunnel.

## Status Check
- **Backend**: Running on port `5086`
- **Frontend**: Running on port `5173`
- **Ngrok**: **CURRENTLY OFFLINE** (Tunnel `https://mica-multifocal-marcell.ngrok-free.dev` not found)

## How to Fix
1. **Start Ngrok**:
   Open a terminal and run:
   ```bash
   ngrok http 5086 --domain=mica-multifocal-marcell.ngrok-free.dev
   ```
   *(If you don't own this domain, just run `ngrok http 5086` and copy the NEW url)*

2. **Update Config**:
   If you get a **new** Ngrok URL:
   - Open `.env` in `frontend` folder.
   - Update `VITE_API_URL` with the new URL.

3. **Restart Frontend**:
   - Updates to `.env` or `api.js` REQUIRE a restart.
   - Stop the server (Ctrl+C).
   - Run `npm run dev`.
