# StockWise - Inventory Management System

StockWise is a multi tenant, role based inventory management web application built for small businesses. Owners can create a business, manage stock across multiple locations, track inventory activity, manage supplier relationships, create purchase orders and interact with an AI powered supplier assistant, all from a single platform.

---

## System Description

StockWise provides the following core features:

- **Multi tenant Business Accounts** - Each owner registers a business. Managers and staff join via invite code.
- **Role based Access** - Three roles: Owner (full access), Manager (inventory + suppliers), Staff (read only inventory).
- **Inventory Management** - Track stock items per location with SKU, quantity, danger range alerts and full activity logging.
- **Inventory Locations** - Owners can manage multiple physical locations, each with its own stock.
- **Suppliers** - Full CRUD supplier profiles stored in MongoDB Atlas, including catalog items (SKU, pricing, pack size), contact info, payment terms, lead time and performance data.
- **Purchase Orders** - Create, edit, submit and receive purchase orders. Receiving automatically updates stock quantities and writes inventory logs.
- **AI Supplier Assistant** - Chat interface powered by Google Gemini that answers natural language questions about your supplier data. Conversations are persisted to the database with a browsable sidebar history.
- **Reports** - Dashboard with inventory health stats, location coverage and stock level summaries.

**Tech Stack:**
- ASP.NET Core 8 Razor Pages (C#)
- Entity Framework Core 8 + Azure SQL Database (inventory, users, orders, chat history)
- MongoDB Atlas (suppliers, inventory logs)
- Google Gemini API (AI chat)
- Bootstrap 5 (UI)

---

## Required Dependencies

### Runtime / SDK
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Access to an **Azure SQL Database** (or any SQL Server instance)
- A **MongoDB Atlas** cluster (free tier works)
- A **Google Gemini API key**

### NuGet Packages (restored automatically via `dotnet restore`)
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Tools`
- `MongoDB.Driver`
- `System.Net.Http` (for Gemini HTTP calls)

---

## Installation & Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd "cs392 demo"
```

### 2. Configure `appsettings.json`

Open `cs392 demo/appsettings.json` and fill in your credentials:

```json
{
  "ConnectionStrings": {
    "cs392_demoContext": "Server=<your-sql-server>;Initial Catalog=<your-db>;User ID=<user>;Password=<password>;Encrypt=True;"
  },
  "AISettings": {
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/",
    "Model": "gemini-2.5-flash",
    "ApiKey": "<your-gemini-api-key>"
  },
  "MongoDBSettings": {
    "ConnectionString": "mongodb+srv://<user>:<password>@<cluster>.mongodb.net/<dbname>?retryWrites=true&w=majority",
    "DatabaseName": "cs392_demo"
  }
}
```

> **Note:** Never commit real credentials to source control. Use environment variables or user secrets for production.

### 3. Apply Database Migrations

Run EF Core migrations to create all SQL tables (including Identity, inventory, purchase orders and chat history):

```bash
cd "cs392 demo"
dotnet ef database update
```

### 4. Restore Packages and Build

```bash
dotnet restore
dotnet build
```

### 5. Run the Application

```bash
dotnet run
```

The application will start at `https://localhost:5095`. On first launch, the `DbSeeder` automatically creates the seed business, roles and demo owner account.

---
### Local / Development

Follow the Installation & Setup steps above. The app runs fully locally as long as the SQL Server and MongoDB Atlas connections are reachable.

---

## Test User Credentials

The following seed account is created automatically on first run:

**Existing accounts to use:**

| Role    | Username                      | Password      |
|---------|-------------------------------|---------------|
| Owner   | CoffeeShopOwner@test.com      | Password123*  |
| Manager | beany@test.com                | Password123*  |
| Staff   | beany02@test.com              | Password123*  |

**To test creation of other roles:**
1. Log in as the Owner (`CoffeeShopOwner@test.com`).
2. Navigate to the **Team Access** page → copy the business **Invite Code**.
3. Register a new account and enter the invite code to join as a staff member.
4. From the Owner account, go to **Settings → Managers** to promote any user to Manager.

---

## Demo Scenarios / Test Cases

### 1. Inventory Management
- Log in as Owner or Manager.
- Go to the **Inventory** page → view all stock items with danger range status indicators.
- Click a stock item → click **Edit** to update quantity; the change is recorded in the Activity Log.
- Go to **Activity Log** to see a timestamped history of all stock changes.

### 2. Supplier Management
- Navigate to the **Suppliers** page → browse the supplier list.
- Click a supplier → view the **Details** tab (contact, address, terms) and **Catalog** tab (SKU, unit, pricing).
- Click **+ New Supplier** to add a supplier with full profile fields.
- Use the **Ask StockWise** button to open the AI assistant pre-scoped to your suppliers and ask any questions related to suppliers or orders in any supplier's catalog.

### 3. Purchase Orders
- Go to **Purchase Orders** → click **+ New Order**.
- Select a supplier, add line items (linked to your stock SKUs), and **Save as Draft** or **Submit Order**.
- On a submitted or draft order, click the **Receive** button to accept delivery. Stock quantities update automatically and an inventory log entry is created (e.g., `PO-0001 (CoffeeShopOwner@test.com)`).

### 4. AI Supplier Assistant
- Navigate to the **Suppliers** page → click **Ask StockWise ✦**.
- Try questions such as:
  - *"Which suppliers have a lead time under 7 days?"*
  - *"Where should I order Almond Milk from?"*
  - *"List all active suppliers vs inactive suppliers."*
- Past conversations are saved and accessible in the left sidebar. Click the trash icon to delete a session.

### 5. Role-based Access
- Staff accounts can view inventory but cannot edit, create or access suppliers/orders.
- Manager accounts have full inventory and supplier access but cannot manage Team Access settings or make an order without the approval of the business owner.
- Owner account has access to all features including business settings, invite code management and manager promotion.

---

## Links

**Deployed System**: (TBD)
**Source Code Repository**: https://github.com/BrettWindhamJU/cs392_Group_Proj

---

## Authors

CS 392 Group Project Team: Eltonia Leonard, Brett Windham and Anthony Munoz

---

## License

This project was created for educational purposes as part of the CS392 Web Driven Application class.
