using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Bridge.Backend.Hubs;
using Bridge.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => 
        policy.WithOrigins(
            "http://localhost:3000", 
            "http://localhost:5000", 
            "http://localhost:5173", 
            "http://localhost:5174", 
            "http://localhost:5175",
            "https://the-bridge-frontend.vercel.app" // Production frontend (Vercel)
        )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// EF Core (PostgreSQL)
builder.Services.AddDbContext<BridgeDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

// JWT (placeholder)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "replace_with_long_secret_key_change_in_production";
builder.Services.AddAuthentication(options => {
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
  options.RequireHttpsMetadata = false;
  options.SaveToken = true;
  options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
  };
});

// Global Authorization Policy - Require authentication by default
builder.Services.AddAuthorization(options => {
  options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
});

// Payment provider (Paystack implementation provided)
builder.Services.AddScoped<Bridge.Backend.Services.IPaymentProvider, Bridge.Backend.Services.PaystackPaymentProvider>();
builder.Services.AddSingleton<Bridge.Backend.Services.MockPaymentProvider>();

// External listing provider
builder.Services.AddHttpClient<Bridge.Backend.Services.ExternalListingProvider>();
builder.Services.AddScoped<Bridge.Backend.Services.IExternalListingProvider, Bridge.Backend.Services.ExternalListingProvider>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MarketplaceHub>("/hub/marketplace");
app.Run();
