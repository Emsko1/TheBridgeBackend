# BRIDGE — Backend (Paystack + Postgres)

This backend is pre-configured to work with Paystack as the payment provider and PostgreSQL as the database.
It includes:
- Listings API (create + list)
- Paystack initialize endpoint (/api/paystack/initialize) which creates an EscrowTransaction and calls Paystack Initialize API
- SignalR hub for marketplace notifications

Important: Replace Paystack secret in appsettings.json or use environment variables in production.

Local development (Docker):
1. docker-compose up -d
2. dotnet build
3. dotnet run (or use the Docker container)
