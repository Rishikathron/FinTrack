#Deployed at Renderer
#LaunchURL https://fintrack-ui.onrender.com/dashboard

#  FinTrack — Personal Net Worth Tracker

A full-stack personal finance application that tracks **Gold**, **Silver**, and **Fixed Deposit** assets with live metal price integration and real-time net worth calculation.

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Phase 1 — Core Application](#phase-1--core-application)
  - [Backend API (.NET 10)](#backend-api-net-10)
  - [Frontend UI (Angular 21)](#frontend-ui-angular-21)
  - [Data Flow](#data-flow)
  - [API Endpoints](#api-endpoints)
  - [Models](#models)
  - [Services & Architecture](#services--architecture)
  - [Storage](#storage)
  - [External API Integration](#external-api-integration)
  - [CORS Configuration](#cors-configuration)
  - [Launch Profiles](#launch-profiles)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running the Backend](#running-the-backend)
  - [Running the Frontend](#running-the-frontend)
  - [Configuration](#configuration)
- [Future Phases](#future-phases)

---

## Tech Stack

| Layer     | Technology                         |
|-----------|------------------------------------|
| Backend   | .NET 10, C# 14, ASP.NET Core Web API |
| Frontend  | Angular 21 (Standalone Components) |
| Storage   | JSON file-based (one file per user)|
| Caching   | In-memory cache (10 min TTL)       |
| Prices    | [MetalPriceAPI](https://metalpriceapi.com/) (with fallback) |
| API Docs  | OpenAPI + Swagger UI               |
| Container | Docker (Linux)                     |

---

## Project Structure

```
FinTrack/
??? FinTrack/                          # .NET Backend
?   ??? Controllers/
?   ?   ??? AssetsController.cs        # Asset CRUD endpoints
?   ?   ??? ValuationController.cs     # Net worth & breakdown endpoints
?   ?   ??? PricesController.cs        # Live metal price endpoint
?   ??? Services/
?   ?   ??? AssetService.cs            # Asset business logic
?   ?   ??? ValuationService.cs        # Valuation calculation engine
?   ??? Interfaces/
?   ?   ??? IAssetService.cs           # Asset service contract
?   ?   ??? IValuationService.cs       # Valuation service contract
?   ?   ??? IPriceProvider.cs          # Price provider contract
?   ??? Models/
?   ?   ??? Asset.cs                   # Asset entity
?   ?   ??? AssetType.cs               # Enum: Gold, Silver, FD
?   ?   ??? AddAssetRequest.cs         # Request DTO for adding assets
?   ?   ??? MetalPrices.cs             # Gold & silver price model
?   ?   ??? NetWorthSummary.cs         # Aggregated net worth response
?   ?   ??? AssetValuation.cs          # Per-asset valuation breakdown
?   ??? Providers/
?   ?   ??? MetalPriceProvider.cs      # MetalPriceAPI integration + cache
?   ??? Storage/
?   ?   ??? JsonFileRepository.cs      # JSON file-based persistence
?   ??? AppData/                       # Runtime data directory (per-user JSON files)
?   ??? Properties/
?   ?   ??? launchSettings.json        # Launch profiles (http, https, IIS Express, Docker)
?   ??? Program.cs                     # App startup, DI, middleware pipeline
?   ??? appsettings.json               # Configuration (API keys, logging)
?   ??? FinTrack.csproj                # Project file (.NET 10)
?
??? fintrack-ui/                       # Angular Frontend
?   ??? src/
?       ??? app/
?       ?   ??? components/
?       ?   ?   ??? dashboard/         # Net worth dashboard (dashboard.ts/html/css)
?       ?   ?   ??? add-asset/         # Add new asset form (add-asset.ts/html/css)
?       ?   ?   ??? asset-list/        # Asset list with delete (asset-list.ts/html/css)
?       ?   ??? services/
?       ?   ?   ??? asset.ts           # HTTP client for /api/assets
?       ?   ?   ??? valuation.ts       # HTTP client for /api/valuation
?       ?   ?   ??? price.ts           # HTTP client for /api/prices
?       ?   ??? models/
?       ?   ?   ??? models.ts          # TypeScript interfaces & enums
?       ?   ??? app.ts                 # Root component
?       ?   ??? app.html               # Root template (navbar + router-outlet)
?       ?   ??? app.css                # Root styles
?       ?   ??? app.routes.ts          # Route definitions
?       ?   ??? app.config.ts          # App providers (router, HttpClient)
?       ??? environments/
?           ??? environment.ts         # API base URL config
?
??? COPILOT_CHANGES.md                 # AI session change log
??? README.md                          # This file
```

---

## Phase 1 — Core Application

Phase 1 delivers the foundational full-stack application with complete asset management, live metal price integration, and a responsive Angular UI.

### Backend API (.NET 10)

The backend is an ASP.NET Core Web API built with **.NET 10** and **C# 14**, following a clean service-oriented architecture designed for future extensibility (Phase 2: Semantic Kernel AI integration).

**Key design decisions:**
- **Interface-driven services** — `IAssetService`, `IValuationService`, `IPriceProvider` enable testability and future Semantic Kernel plugin compatibility.
- **Strategy-style valuation** — `ValuationService` uses a `switch` expression per `AssetType`, making it easy to add new asset types (Crypto, Stocks, Mutual Funds).
- **Fallback pricing** — If the MetalPriceAPI key is missing or the API is down, hardcoded fallback prices are used (`Gold: ?7,500/g`, `Silver: ?90/g`).
- **In-memory caching** — Metal prices are cached for 10 minutes to reduce API calls.

### Frontend UI (Angular 21)

The frontend is built with **Angular 21** using **standalone components** and **signals** for reactive state management.

**Pages:**

| Route          | Component    | Description                              |
|----------------|-------------|------------------------------------------|
| `/dashboard`   | `Dashboard`  | Displays net worth summary and live metal prices |
| `/add-asset`   | `AddAsset`   | Form to add Gold, Silver, or FD assets   |
| `/assets`      | `AssetList`  | Lists all assets with delete functionality |

**Features:**
- Angular Signals for reactive state (`signal`, `signal.update`)
- `FormsModule` for template-driven add-asset form
- `HttpClient` for REST API communication
- Environment-based API URL configuration
- Currency formatting in INR (?) with Indian locale

### Data Flow

```
????????????????      HTTP/REST       ????????????????????
?  Angular UI  ? ???????????????????? ?  ASP.NET Core API ?
?  :4200       ?                      ?  :44380 (IIS Exp) ?
????????????????                      ????????????????????
                                               ?
                        ???????????????????????????????????????????
                        ?                      ?                  ?
                  ??????????????    ?????????????????   ???????????????????
                  ?AssetService?    ?ValuationService?   ?MetalPriceProvider?
                  ??????????????    ?????????????????   ???????????????????
                        ?                   ?                     ?
                  ??????????????????        ?            ??????????????????
                  ?JsonFileRepo    ??????????            ?MetalPriceAPI   ?
                  ?(AppData/*.json)?                      ?(+ MemoryCache) ?
                  ??????????????????                      ??????????????????
```

### API Endpoints

| Method   | Route                       | Controller           | Description                          |
|----------|-----------------------------|----------------------|--------------------------------------|
| `GET`    | `/api/assets`               | `AssetsController`   | Get all assets for the current user  |
| `POST`   | `/api/assets`               | `AssetsController`   | Add a new asset                      |
| `DELETE` | `/api/assets/{id:guid}`     | `AssetsController`   | Delete an asset by ID                |
| `GET`    | `/api/valuation/networth`   | `ValuationController`| Get total net worth summary          |
| `GET`    | `/api/valuation/breakdown`  | `ValuationController`| Get per-asset valuation breakdown    |
| `GET`    | `/api/prices/current`       | `PricesController`   | Get current gold & silver prices/gram|

> **Note**: All asset endpoints use a hardcoded `default-user` user ID. Authentication will be added in a future phase.

### Models

#### Backend (C#)

| Model              | Purpose                                      |
|--------------------|----------------------------------------------|
| `Asset`            | Entity with Id, Type, Quantity, Amount, Unit, CreatedAt |
| `AssetType`        | Enum: `Gold`, `Silver`, `FD`                 |
| `AddAssetRequest`  | DTO for POST `/api/assets` body              |
| `MetalPrices`      | Gold & silver price per gram + fetch timestamp|
| `NetWorthSummary`  | Aggregated values: GoldValue, SilverValue, FDValue, TotalNetWorth |
| `AssetValuation`   | Per-asset breakdown: AssetId, Type, Quantity, PricePerUnit, TotalValue |

#### Frontend (TypeScript)

| Interface/Enum     | File               | Maps to Backend Model |
|--------------------|--------------------|-----------------------|
| `AssetType` (enum) | `models/models.ts` | `AssetType`           |
| `Asset`            | `models/models.ts` | `Asset`               |
| `AddAssetRequest`  | `models/models.ts` | `AddAssetRequest`     |
| `MetalPrices`      | `models/models.ts` | `MetalPrices`         |
| `NetWorthSummary`  | `models/models.ts` | `NetWorthSummary`     |
| `AssetValuation`   | `models/models.ts` | `AssetValuation`      |

### Services & Architecture

#### Backend Services

| Service                | Interface           | Responsibility                                         |
|------------------------|---------------------|-------------------------------------------------------|
| `AssetService`         | `IAssetService`     | CRUD operations on assets via `JsonFileRepository`     |
| `ValuationService`     | `IValuationService` | Calculates net worth & per-asset valuations using live prices |
| `MetalPriceProvider`   | `IPriceProvider`    | Fetches gold/silver prices from MetalPriceAPI with 10-min cache and fallback |

**DI Registration (Program.cs):**
```csharp
builder.Services.AddSingleton<JsonFileRepository>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IValuationService, ValuationService>();
builder.Services.AddHttpClient<IPriceProvider, MetalPriceProvider>();
builder.Services.AddMemoryCache();
```

#### Frontend Services

| Service            | File                     | API Base Route        |
|--------------------|--------------------------|-----------------------|
| `AssetService`     | `services/asset.ts`      | `/api/assets`         |
| `ValuationService` | `services/valuation.ts`  | `/api/valuation`      |
| `PriceService`     | `services/price.ts`      | `/api/prices`         |

### Storage

Assets are stored as **JSON files** in the `FinTrack/AppData/` directory, one file per user:

```
FinTrack/AppData/
??? default-user.json    # JSON array of Asset objects
```

The `JsonFileRepository` provides thread-safe read/write with a static lock object. Example file content:

```json
[
  {
    "id": "a1b2c3d4-...",
    "type": "Gold",
    "quantity": 10.5,
    "amount": 0,
    "unit": "grams",
    "createdAt": "2025-01-15T10:30:00Z"
  },
  {
    "id": "e5f6g7h8-...",
    "type": "FD",
    "quantity": 0,
    "amount": 500000,
    "unit": "INR",
    "createdAt": "2025-01-15T11:00:00Z"
  }
]
```

### External API Integration

**MetalPriceAPI** ([metalpriceapi.com](https://metalpriceapi.com/))

| Detail         | Value                                           |
|----------------|-------------------------------------------------|
| Endpoint       | `https://api.metalpriceapi.com/v1/latest`       |
| Parameters     | `api_key`, `base=INR`, `currencies=XAU,XAG`     |
| Response       | Rates in ounces per INR ? converted to ?/gram   |
| Cache Duration | 10 minutes (in-memory)                           |
| Fallback       | Gold: ?7,500/g, Silver: ?90/g                   |
| Config Key     | `MetalPriceApi:ApiKey` in `appsettings.json`     |

**Conversion logic:**
```
Price per ounce (INR) = 1 / rate
Price per gram (INR)  = Price per ounce / 31.1035
```

### CORS Configuration

The API is configured to accept cross-origin requests from the Angular dev server:

```csharp
policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
```

**Middleware pipeline order:**
1. `UseCors()` — Handles CORS preflight and headers first
2. `UseHttpsRedirection()` — Only in non-Development environments
3. `UseAuthorization()`
4. `MapControllers()`

> **Important**: `UseHttpsRedirection` is disabled in Development to prevent broken redirects when running the HTTP-only profile.

### Launch Profiles

| Profile       | URL(s)                                              | Use Case              |
|---------------|-----------------------------------------------------|-----------------------|
| `http`        | `http://localhost:5130`                              | Local dev (HTTP only) |
| `https`       | `https://localhost:7214` + `http://localhost:5130`   | Local dev (HTTPS)     |
| `IIS Express` | `http://localhost:61776` / `https://localhost:44380` | Visual Studio default |
| `Docker`      | `http://localhost:8080` / `https://localhost:8081`   | Container deployment  |

> **Note**: Update `fintrack-ui/src/environments/environment.ts` to match the active launch profile URL.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (v18+ recommended)
- [Angular CLI](https://angular.dev/) (`npm install -g @angular/cli`)
- (Optional) [MetalPriceAPI key](https://metalpriceapi.com/) for live metal prices

### Running the Backend

```bash
cd FinTrack
dotnet run
```

Or launch from Visual Studio using any profile (http, https, IIS Express).

The API will be available at the URL corresponding to your launch profile. Swagger UI is available at `/swagger` in Development mode.

### Running the Frontend

```bash
cd fintrack-ui
npm install
ng serve
```

The Angular app will be available at `http://localhost:4200`.

### Configuration

#### API Key (Optional)

Add your MetalPriceAPI key to `FinTrack/appsettings.json`:

```json
{
  "MetalPriceApi": {
    "ApiKey": "your-api-key-here"
  }
}
```

Without an API key, the app uses fallback prices (Gold: ?7,500/g, Silver: ?90/g).

#### Angular API URL

Update `fintrack-ui/src/environments/environment.ts` to match your backend URL:

```typescript
export const environment = {
  apiBaseUrl: 'https://localhost:44380/api'  // Match your active launch profile
};
```

| Launch Profile | `apiBaseUrl` Value               |
|----------------|----------------------------------|
| http           | `http://localhost:5130/api`       |
| https          | `https://localhost:7214/api`      |
| IIS Express    | `https://localhost:44380/api`     |
| Docker         | `https://localhost:8081/api`      |

---

## Future Phases

| Phase | Feature                          | Description                                                  |
|-------|----------------------------------|--------------------------------------------------------------|
| 2     | Semantic Kernel AI Integration   | AI-powered financial insights using service interfaces as SK plugins |
| 3     | Authentication & Multi-user      | Replace hardcoded `default-user` with proper auth (JWT/OAuth)|
| 4     | Additional Asset Types           | Mutual Funds, Stocks, Crypto support                         |
| 5     | Database Migration               | Move from JSON files to SQL/NoSQL database                   |
| 6     | Azure Deployment                 | Deploy to Azure App Service with managed identity            |
