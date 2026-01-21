-- Initial schema for BRIDGE MVP (Postgres)
CREATE TABLE IF NOT EXISTS "Users" (
  "Id" UUID PRIMARY KEY,
  "Name" TEXT,
  "Email" TEXT UNIQUE,
  "Role" TEXT,
  "KycStatus" TEXT,
  "PasswordHash" TEXT
);

CREATE TABLE IF NOT EXISTS "Listings" (
  "Id" UUID PRIMARY KEY,
  "SellerId" UUID REFERENCES "Users"("Id"),
  "Type" TEXT,
  "Title" TEXT,
  "Description" TEXT,
  "Price" NUMERIC,
  "Year" INT,
  "Location" TEXT,
  "Photos" JSONB,
  "Status" TEXT
);

CREATE TABLE IF NOT EXISTS "EscrowTransactions" (
  "Id" UUID PRIMARY KEY,
  "ListingId" UUID REFERENCES "Listings"("Id"),
  "BuyerId" UUID REFERENCES "Users"("Id"),
  "SellerId" UUID REFERENCES "Users"("Id"),
  "Amount" NUMERIC,
  "Fee" NUMERIC,
  "Currency" TEXT,
  "Status" TEXT,
  "PaymentProvider" TEXT,
  "ProviderReference" TEXT,
  "CreatedAt" TIMESTAMP
);
