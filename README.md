# UniStyle — E-Commerce Backend API

> A RESTful backend for an online fashion store, built with ASP.NET Core 8 and MongoDB.

---

## Overview

UniStyle is a backend system that powers an online clothing shop. It handles product browsing, order management with PayPal payment integration, user authentication, and admin reporting. The system serves two main groups: **customers** who shop online and **admins** who manage the store.

---

## Features

### Customer
- Register and log in with JWT-based authentication
- Browse products with pagination and filter by category, color, size
- Place orders with variant selection (color + size)
- Pay via PayPal; receive email confirmation on delivery

### Admin
- Full CRUD for products (with Cloudinary image upload), categories, colors, and sizes
- Manage customers and orders (update payment/delivery status)
- View revenue and quantity statistics by day, month, or year

---

## Tech Stack

| Layer              | Technology                          |
|--------------------|--------------------------------------|
| **Backend**        | ASP.NET Core 8.0 (Web API + Razor Pages) |
| **Database**       | MongoDB (via MongoDB.Driver 3.4)    |
| **Authentication** | JWT Bearer tokens (stored in cookies) |
| **Image Storage**  | Cloudinary                          |
| **Payment**        | PayPal Checkout SDK                 |
| **Email**          | SMTP Email Service                  |
| **API Docs**       | Swagger / OpenAPI (Swashbuckle)     |

---

## Architecture

Monolithic application using a layered structure:

```
Request → Controller → Service → MongoDB
```

- **Controllers** — handle HTTP routing, validation, and response
- **Services** — encapsulate business logic (JWT, PayPal, Cloudinary, Email)
- **Models** — define MongoDB document schemas (BSON-mapped entities)
- **Middleware** — custom authentication middleware for cookie-based JWT
- **Razor Pages** — server-rendered admin dashboard

---

## API Overview

**Base URL:** `http://localhost:{PORT}/api`

**Auth:** JWT Bearer token (also accepted via `accessToken` cookie)

| Method | Endpoint                       | Auth     | Description                     |
|--------|--------------------------------|----------|---------------------------------|
| POST   | `/api/account/login`           | Public   | Login and receive JWT           |
| GET    | `/api/account/user-info`       | User     | Get current user info           |
| POST   | `/api/customer`                | Public   | Register new user               |
| GET    | `/api/products`                | Public   | List products (paginated)       |
| POST   | `/api/products`                | Admin    | Create product with images      |
| POST   | `/api/orders`                  | User     | Place an order                  |
| POST   | `/api/orders/capture`          | User     | Capture PayPal payment          |
| GET    | `/api/orders`                  | Admin    | List all orders (paginated)     |
| GET    | `/api/chart/stats`             | Admin    | Revenue & quantity statistics   |

> Full API docs available via Swagger UI at `/swagger` when running in Development mode.

---

## Installation & Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- MongoDB instance (local or Atlas)
- Cloudinary account
- PayPal sandbox credentials

### Steps

```bash
# 1. Clone the repository
git clone https://github.com/nguyen-gia-khoi/LTWNC.git
cd LTWNC/LTWNC

# 2. Restore dependencies
dotnet restore

# 3. Configure environment variables (see below)

# 4. Run the project
dotnet run
```

The API will be available at `https://localhost:7xxx` and Swagger UI at `/swagger`.

---

## Environment Variables

Create or update `appsettings.json` (or use User Secrets / environment variables):

```json
{
  "AppSettings": {
    "BaseUrl": ""
  },
  "JwtConfig": {
    "Key": "",
    "Issuer": "",
    "Audience": ""
  },
  "MongoDbSettings": {
    "ConnectionString": "",
    "DatabaseName": ""
  },
  "CloudinarySettings": {
    "CloudName": "",
    "ApiKey": "",
    "ApiSecret": ""
  },
  "PayPal": {
    "ClientId": "",
    "ClientSecret": "",
    "Mode": "sandbox"
  },
  "EmailSettings": {
    "SmtpHost": "",
    "SmtpPort": "",
    "SenderEmail": "",
    "SenderPassword": ""
  }
}
```

---

## Project Structure

```
LTWNC/
├── Controllers/        # API endpoints (Products, Orders, Account, etc.)
├── Services/           # Business logic (JWT, PayPal, Cloudinary, Email)
├── Models/
│   ├── Entities/       # MongoDB document models
│   └── API/            # Request/response DTOs
├── Data/               # MongoDB connection service
├── Middleware/         # Custom authentication middleware
├── Pages/              # Razor Pages (admin dashboard)
└── Program.cs          # App configuration and DI setup
```

---

## Future Improvements

- **Search & Filtering** — full-text product search with category/price filters
- **Cart persistence** — store cart server-side linked to user accounts
- **Refresh tokens** — implement token rotation for better session security
- **Caching** — add Redis caching for frequently accessed product listings
- **Docker support** — containerize the app and database for consistent deployments
