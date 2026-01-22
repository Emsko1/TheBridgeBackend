# Frontend Fix - URGENT

Your frontend is still showing "Cannot connect to backend" because the Vercel environment variable is not set correctly.

## STEP 1: Go to Vercel Dashboard
https://vercel.com/dashboard

## STEP 2: Select Your Project
Click on "TheBridgeFrontend" project

## STEP 3: Go to Settings
Settings → Environment Variables

## STEP 4: Add/Update Variable
- Name: VITE_API_URL
- Value: https://thebridgebackend.onrender.com

## STEP 5: Redeploy
Deployments → Click latest deployment → Redeploy

## STEP 6: Wait for Build to Complete
Once deployed, go to your frontend and try registering again.

---

Backend URL: https://thebridgebackend.onrender.com
Frontend URL: https://the-bridge-frontend.vercel.app
