# MyFinanceTracker

[![Build](https://github.com/Linnod/MyFinanceTracker/actions/workflows/ci.yml/badge.svg)](https://github.com/Linnod/MyFinanceTracker/actions/workflows/ci.yml)
[![.NET 11](https://img.shields.io/badge/.NET-11.0-blue)]()

Personal finance tracker that stores transactions in Google Sheets and supports multimodal expense tracking (text, voice, receipt photos, banking screenshots, and PDF documents) through Telegram bot interface.

## Features

- Complete natural language transaction lifecycle: create, list/filter, update, and delete
- Multimodal transaction input: text commands, voice messages, receipt photos, banking screenshots, and PDF documents (receipts, invoices, and bank statements)
- Gemini-powered natural language processing, audio transcription, image analysis, and PDF document extraction
- Local SQLite persistence with Transactional Outbox pattern
- Resilient background synchronization to Google Sheets (retry policy & dead-letter queue)
- Category management synchronized with Google Sheets and SQLite
- Telegram Bot interaction mode
- Clean Architecture + CQRS (MediatR)
- Structured logging with correlation IDs

## Architecture

```mermaid
flowchart LR
    User["User"]

    Interactions["Interactions
    (Telegram)"]

    Parser["Input Processing"]

    Gemini["Gemini API"]

    UseCases["Use Cases"]

    Sqlite[("SQLite DB
    & Outbox")]

    Sync["Outbox Sync
    Worker"]

    Sheets["Google Sheets"]

    User --> Interactions
    Interactions --> Parser

    Parser --> Gemini

    Gemini --> UseCases

    UseCases --> Sqlite
    Sqlite -.-> Sync
    Sync --> Sheets
```

## Project Structure

```text
src/
├── MyFinanceTracker.Domain/
├── MyFinanceTracker.Common/
├── MyFinanceTracker.UseCases/
├── MyFinanceTracker.Infrastructure.Persistence/
├── MyFinanceTracker.InputProcessing/
├── MyFinanceTracker.InputProcessing.Gemini/
├── MyFinanceTracker.Interactions.Telegram/
└── MyFinanceTracker.Host/

tests/
├── MyFinanceTracker.Domain.Tests/
├── MyFinanceTracker.UseCases.Tests/
├── MyFinanceTracker.Infrastructure.Persistence.Tests/
├── MyFinanceTracker.InputProcessing.Gemini.Tests/
└── MyFinanceTracker.Interactions.Telegram.Tests/
```

## Quick Start

### 1. Clone the repository

```bash
git clone https://github.com/Linnod/MyFinanceTracker.git
cd MyFinanceTracker
```

### 2. Create your Google Sheets tracker

Create your own spreadsheet from the template:

https://docs.google.com/spreadsheets/d/1wt-her1rbfA4PK5t4Ilyfe0D0BNcf7TYhMIJKZrDtTc/copy

The template contains two flat sheets:

- `Transactions` with columns: `Id`, `Date`, `Type`, `Category`, `Amount`, `Note`
- `Categories` with columns: `Id`, `Name`, `IsArchived`

The header row (row 1) on both sheets is required and must not be deleted.

### 3. Create a Google Service Account

1. Create a Google Cloud project.
2. Enable the Google Sheets API.
3. Create a Service Account.
4. Download the credentials JSON file and save it as `google-api-credentials.json` in the repository root.

### 4. Share the spreadsheet

Share your copied spreadsheet with the Service Account email address and grant it **Editor** permissions.

### 5. Configure categories

Categories are stored on the `Categories` sheet in your Google Spreadsheet (with columns: `Id`, `Name`, `IsArchived`). You can start in either of two ways:

- **Pre-populate `Categories` in Google Sheets:** On first startup with an empty local SQLite database, existing categories (and transactions) are imported from Google Sheets.
- **Start with an empty `Categories` sheet:** Leave only the header row (row 1), and create categories later through the Telegram bot—they will be synchronized to Google Sheets via the outbox.

Once the local SQLite database is populated, subsequent startups skip remote import and sync local changes to Google Sheets via the outbox.

### 6. Configure the application

Create `src/MyFinanceTracker.Host/appsettings.Development.json`:

```json
{
  "GoogleSheets": {
    "SpreadsheetId": "YOUR_SPREADSHEET_ID"
  },
  "Telegram": {
    "Token": "YOUR_TELEGRAM_BOT_TOKEN",
    "AllowedUserId": 123456789
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "YOUR_GEMINI_MODEL"
  }
}
```

> [!NOTE]
> `GoogleSheets:CredentialsFilePath` defaults to `google-api-credentials.json` in `src/MyFinanceTracker.Host/appsettings.json` and resolves relative to the current working directory (the repository root when running the command below). If you launch from an IDE whose working directory is `src/MyFinanceTracker.Host/`, set `GoogleSheets:CredentialsFilePath` to `../../google-api-credentials.json` or an absolute path.

### 7. Run the application

```bash
dotnet build
dotnet run --project src/MyFinanceTracker.Host
```

## Configuration

### Full Configuration Example

```json
{
  "GoogleSheets": {
    "SpreadsheetId": "YOUR_SPREADSHEET_ID",
    "CredentialsFilePath": "google-api-credentials.json",
    "TransactionsSheetName": "Transactions",
    "CategoriesSheetName": "Categories",
    "ApplicationName": "MyFinanceTracker"
  },
  "SqlitePersistence": {
    "ConnectionString": "Data Source=finance.db",
    "SyncIntervalSeconds": 60,
    "MaxRetryAttempts": 5
  },
  "Telegram": {
    "Token": "YOUR_TELEGRAM_BOT_TOKEN",
    "AllowedUserId": 123456789
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "YOUR_GEMINI_MODEL"
  }
}
```

### Category Configuration Notes

> [!IMPORTANT]
> Category `id` is a unique identifier (slug) recorded on the `Transactions` sheet in Google Sheets, while `name` is used as the display name for Telegram messages and Gemini prompts.

### Telegram Settings

Make sure that:

- `Telegram:Token` is configured.
- `Telegram:AllowedUserId` contains your Telegram user ID.

## Docker Deployment

You can also run the application as a background service via Docker Compose:

1. Prepare configuration files, persistent data directory, and secrets:
   ```bash
   cp .env.example .env
   mkdir -p deploy/data
   cp google-api-credentials.json deploy/google-api-credentials.json
   ```

2. Fill in `.env` with your Telegram, Google Sheets, and Gemini credentials.

3. Build the local container image (or use `-f docker-compose.yml -f docker-compose.deploy.yml` in the next step to pull the pre-built image from GHCR):
   ```bash
   dotnet publish src/MyFinanceTracker.Host/MyFinanceTracker.Host.csproj -c Release /t:PublishContainer
   ```

4. Start the container:
   ```bash
   docker compose up -d
   ```

> [!NOTE]
> The SQLite database and outbox queue are stored in `deploy/data/finance.db` via a mounted Docker volume, ensuring transaction data and sync state persist across container updates and restarts.

## Usage Examples

### Text Commands

Natural language input in any language for managing transactions and categories:

```text
# Add transactions
Spent 15.50 on groceries
Spent 8.40 at Starbucks yesterday
Received salary 2500

# View transactions
Show transactions for yesterday
View food expenses for last week
Show October transactions

# Update a transaction (by ID)
Update transaction 7d9a8c12-34ef-5678-90ab-cdef12345678: 18.50 cafe
Change transaction 7d9a8c12-34ef-5678-90ab-cdef12345678: remove note

# Delete transactions (by ID)
Delete transaction 7d9a8c12-34ef-5678-90ab-cdef12345678
Delete transactions 7d9a8c12-34ef-5678-90ab-cdef12345678 and 1a2b3c4d-5678-90ab-cdef-1234567890ab

# Manage categories
Show my categories
Create category Subscriptions
Rename category Subscriptions to Streaming
Archive category Streaming
Unarchive category Streaming
```

### Voice Messages (Telegram)

Send voice messages directly to the Telegram bot in any language:

> *"Spent 12 euros on lunch at Subway"*  
> *"Bought coffee and a croissant for 7.50"*

### Receipts & Screenshots (Telegram)

Send images directly to the bot:

- **Receipts**: Automatically extracts the total amount, infers the category from the merchant/items, and extracts the transaction date.
- **Banking App Screenshots**: Recognizes transaction details from apps such as Revolut, Monzo, etc.
- **Optional Captions**: Attach a text caption to clarify or override details (e.g. `Pharmacy yesterday` or `Groceries split with friend`).

### PDF Documents (Telegram)

Send PDF files (up to 20 MB) directly to the bot:

- **Receipts & Invoices**: Automatically extracts the total amount as a single transaction (unless instructed otherwise in a caption), merchant/store name, and transaction date.
- **Bank Receipts & Statements**: Extracts one or more transactions from bank statements (e.g., Revolut statements), inferring expense vs. income direction and ignoring balances or summary rows.
- **Optional Captions**: Attach a text caption to clarify or override details (e.g. `Split specific items` or `Business lunch`).
