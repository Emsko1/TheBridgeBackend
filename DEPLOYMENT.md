# TheBridge Backend - Deployment Guide

## Deploying to Render

### Prerequisites
- GitHub account with this repository
- Render account (https://render.com)
- PostgreSQL database setup

### Environment Variables Required on Render

Set these environment variables in your Render service:

1. **ConnectionStrings__DefaultConnection** - PostgreSQL connection string
   ```
   Host=<your-postgres-host>;Port=5432;Database=<db-name>;Username=<user>;Password=<password>
   ```

2. **Jwt__Key** - JWT secret key for authentication (use a strong, random key)
   ```
   your-long-random-secret-key-min-32-chars
   ```

3. **Paystack__SecretKey** - Paystack API secret key
   ```
   sk_live_xxxxxxxxxxxx
   ```

4. **Frontend__BaseUrl** - Frontend application URL
   ```
   https://your-frontend-domain.com
   ```

5. **ASPNETCORE_ENVIRONMENT** - Set to `Production`

### Deployment Steps

1. **Connect to GitHub**
   - Go to https://dashboard.render.com
   - Click "New +" → "Web Service"
   - Connect your GitHub account
   - Select this repository

2. **Configure Service**
   - Name: `thebridge-backend`
   - Environment: `Docker`
   - Build Command: Leave empty (Dockerfile handles it)
   - Start Command: `dotnet bridge.backend.dll`
   - Plan: Free or paid based on your needs

3. **Add Environment Variables**
   - In Render dashboard, go to "Environment"
   - Add all required variables listed above

4. **Connect Database**
   - If using Render PostgreSQL:
     - Create a PostgreSQL database on Render
     - Copy the internal connection string
     - Add it as `ConnectionStrings__DefaultConnection`

5. **Deploy**
   - Click "Create Web Service"
   - Render will automatically build and deploy

### CORS Configuration

Update `Program.cs` CORS settings if needed. Currently allows:
- http://localhost:3000
- http://localhost:5000
- http://localhost:5173
- http://localhost:5174
- http://localhost:5175

For production, update with your frontend domain.

### Health Check

Test the deployment:
```bash
curl https://your-service.onrender.com/health
```

### Troubleshooting

- Check Render logs for deployment errors
- Ensure PostgreSQL is accessible from Render
- Verify all environment variables are set correctly
- Check that CORS origins include your frontend URL

### Rebuilding

Changes pushed to GitHub will automatically trigger new builds on Render.

