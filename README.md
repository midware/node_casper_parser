Node Casper Parser by Kamil Szymoniak


# 🔍 Node Casper Parser

<div align="center">

![.NET Version](https://img.shields.io/badge/.NET-6.0%2B-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)
![Casper Network](https://img.shields.io/badge/Casper-Network-FF0000)
![API](https://img.shields.io/badge/API-REST-blue)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D)

**C# .NET Casper Network Node Indexer with REST API & Swagger Documentation**

[Live Mainnet API](https://api.node.casper.mystra.io/swagger) •
[Live Testnet API](https://api.testnet.node.casper.io/swagger) •
[Documentation](#-documentation)

</div>

---

## 📋 Table of Contents

- [About](#-about)
- [Features](#-features)
- [Live Instances](#-live-instances)
- [Architecture](#-architecture)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [API Documentation](#-api-documentation)
- [Usage Examples](#-usage-examples)
- [Database Schema](#-database-schema)
- [Performance](#-performance)
- [Contributing](#-contributing)
- [License](#-license)
- [Author](#-author)

---

## 🎯 About

**Node Casper Parser** is a high-performance blockchain indexer for the Casper Network built with C# .NET. It continuously monitors and indexes blockchain data, providing a REST API with Swagger documentation for easy integration with dApps, wallets, and analytics platforms.

### ✨ Key Features:

- ✅ **Real-time Indexing** - Continuously monitors Casper Network nodes
- ✅ **REST API** - Full-featured REST API with OpenAPI specification
- ✅ **Swagger UI** - Interactive API documentation and testing
- ✅ **High Performance** - Optimized database queries and caching
- ✅ **Multi-Network** - Supports both Mainnet and Testnet
- ✅ **Scalable Architecture** - Microservices-ready design
- ✅ **Production Ready** - Battle-tested on live networks

---

## 🚀 Features

| Feature | Description |
|---------|-------------|
| **Block Indexing** | Index all blocks with full transaction data |
| **Deploy Tracking** | Track and query all deploys and their status |
| **Account Monitoring** | Monitor account balances and activities |
| **Transfer History** | Complete transfer history with pagination |
| **Validator Data** | Validator performance and statistics |
| **Event Streaming** | Real-time event notifications via SSE |
| **Search API** | Advanced search across blocks, deploys, accounts |
| **Analytics** | Network statistics and metrics |

---

## 🌐 Live Instances

### **Mainnet API**
https://api.node.casper.mystra.io/swagger

text
- Full Casper Mainnet indexing
- Production-grade performance
- 99.9% uptime SLA

### **Testnet API**
https://api.testnet.node.casper.io/swagger

text
- Casper Testnet environment
- Perfect for development and testing
- Latest network upgrades

---

## 🏗️ Architecture

┌─────────────────────────────────────────────────────────────┐
│ Casper Network Nodes │
│ (RPC + SSE Endpoints) │
└─────────────────────────────────────────────────────────────┘
↓
┌─────────────────────────────────────────────────────────────┐
│ Node Casper Parser Service │
│ │
│ ┌──────────────┐ ┌──────────────┐ ┌─────────────────┐ │
│ │ Block │ │ Deploy │ │ Event │ │
│ │ Indexer │ │ Processor │ │ Listener │ │
│ └──────────────┘ └──────────────┘ └─────────────────┘ │
│ ↓ │
│ ┌────────────────────────────────────────────────────┐ │
│ │ Database Layer (SQL Server / PostgreSQL) │ │
│ └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
↓
┌─────────────────────────────────────────────────────────────┐
│ REST API Layer │
│ (ASP.NET Core Web API) │
│ │
│ ┌──────────────┐ ┌──────────────┐ ┌─────────────────┐ │
│ │ Blocks │ │ Deploys │ │ Accounts │ │
│ │ Controller │ │ Controller │ │ Controller │ │
│ └──────────────┘ └──────────────┘ └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
↓
┌─────────────────────────────────────────────────────────────┐
│ Swagger UI / OpenAPI Docs │
└─────────────────────────────────────────────────────────────┘

text

---

## 📦 Installation

### Prerequisites

- **.NET 6.0 SDK** or higher
- **SQL Server 2019+** or **PostgreSQL 12+**
- **Access to Casper Network Node** (RPC endpoint)

### 1. Clone Repository

```bash
git clone https://github.com/midware/node_casper_parser.git
cd node_casper_parser
2. Restore Dependencies
bash
dotnet restore
3. Configure Database
Update appsettings.json with your database connection:

json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CasperParser;User Id=sa;Password=YourPassword;"
  }
}
4. Run Database Migrations
bash
dotnet ef database update
5. Build & Run
bash
dotnet build
dotnet run
The API will be available at: https://localhost:5001/swagger

Environment Variables
bash
# Casper Node
export CASPER_RPC_URL=http://localhost:7777/rpc
export CASPER_EVENT_URL=http://localhost:9999/events

# Database
export DB_CONNECTION_STRING="Server=localhost;Database=CasperParser;..."

# API
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=https://+:443;http://+:80
📚 API Documentation
Base URL
text
Mainnet: https://api.node.casper.mystra.io
Testnet: https://api.testnet.node.casper.io
Key Endpoints
Blocks
text
GET /api/v1/blocks
GET /api/v1/blocks/{blockHash}
GET /api/v1/blocks/height/{height}
GET /api/v1/blocks/latest
Deploys
text
GET /api/v1/deploys
GET /api/v1/deploys/{deployHash}
GET /api/v1/deploys/account/{accountHash}
GET /api/v1/deploys/status/{deployHash}
Accounts
text
GET /api/v1/accounts/{accountHash}
GET /api/v1/accounts/{accountHash}/balance
GET /api/v1/accounts/{accountHash}/transfers
GET /api/v1/accounts/{accountHash}/deploys
Transfers
text
GET /api/v1/transfers
GET /api/v1/transfers/{transferId}
GET /api/v1/transfers/from/{accountHash}
GET /api/v1/transfers/to/{accountHash}
Validators
text
GET /api/v1/validators
GET /api/v1/validators/{publicKey}
GET /api/v1/validators/{publicKey}/rewards
GET /api/v1/validators/era/{eraId}
Network Stats
text
GET /api/v1/stats/network
GET /api/v1/stats/validators
GET /api/v1/stats/activity
💻 Usage Examples
Example 1: Get Latest Block
bash
curl -X GET "https://api.node.casper.mystra.io/api/v1/blocks/latest" \
  -H "Accept: application/json"
Response:

json
{
  "blockHash": "3f8b1c2...",
  "parentHash": "7a4d9e5...",
  "height": 1523456,
  "timestamp": "2026-01-13T19:00:00Z",
  "eraId": 8432,
  "proposer": "01a1b2c3...",
  "deployCount": 15,
  "transferCount": 8
}
Example 2: Query Deploys by Account
bash
curl -X GET "https://api.node.casper.mystra.io/api/v1/deploys/account/account-hash-abc123...?page=1&pageSize=20" \
  -H "Accept: application/json"
Response:

json
{
  "data": [
    {
      "deployHash": "4c3d2e1...",
      "accountHash": "account-hash-abc123...",
      "timestamp": "2026-01-13T18:55:00Z",
      "cost": "2500000000",
      "status": "executed"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 145,
    "totalPages": 8
  }
}
Example 3: C# Client Integration
csharp
using System.Net.Http;
using System.Net.Http.Json;

public class CasperParserClient
{
    private readonly HttpClient _client;
    
    public CasperParserClient(string baseUrl)
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }
    
    public async Task<BlockInfo> GetLatestBlockAsync()
    {
        return await _client.GetFromJsonAsync<BlockInfo>("/api/v1/blocks/latest");
    }
    
    public async Task<DeployInfo> GetDeployAsync(string deployHash)
    {
        return await _client.GetFromJsonAsync<DeployInfo>($"/api/v1/deploys/{deployHash}");
    }
}

// Usage
var client = new CasperParserClient("https://api.node.casper.mystra.io");
var latestBlock = await client.GetLatestBlockAsync();
Console.WriteLine($"Latest block height: {latestBlock.Height}");
🗄️ Database Schema
Core Tables
sql
-- Blocks
CREATE TABLE Blocks (
    Id BIGINT PRIMARY KEY IDENTITY,
    BlockHash VARCHAR(64) NOT NULL UNIQUE,
    ParentHash VARCHAR(64) NOT NULL,
    Height BIGINT NOT NULL UNIQUE,
    Timestamp DATETIME2 NOT NULL,
    EraId INT NOT NULL,
    ProposerPublicKey VARCHAR(68) NOT NULL,
    StateRootHash VARCHAR(64) NOT NULL,
    DeployCount INT NOT NULL,
    TransferCount INT NOT NULL,
    INDEX IX_Height (Height),
    INDEX IX_Timestamp (Timestamp)
);

-- Deploys
CREATE TABLE Deploys (
    Id BIGINT PRIMARY KEY IDENTITY,
    DeployHash VARCHAR(64) NOT NULL UNIQUE,
    AccountHash VARCHAR(68) NOT NULL,
    BlockHash VARCHAR(64) NULL,
    Timestamp DATETIME2 NOT NULL,
    Cost DECIMAL(28,0) NOT NULL,
    Status VARCHAR(20) NOT NULL,
    ExecutionResult NVARCHAR(MAX) NULL,
    INDEX IX_AccountHash (AccountHash),
    INDEX IX_BlockHash (BlockHash),
    INDEX IX_Timestamp (Timestamp)
);

-- Transfers
CREATE TABLE Transfers (
    Id BIGINT PRIMARY KEY IDENTITY,
    TransferId VARCHAR(64) NOT NULL UNIQUE,
    DeployHash VARCHAR(64) NOT NULL,
    FromAccount VARCHAR(68) NOT NULL,
    ToAccount VARCHAR(68) NOT NULL,
    Amount DECIMAL(28,0) NOT NULL,
    BlockHeight BIGINT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    INDEX IX_FromAccount (FromAccount),
    INDEX IX_ToAccount (ToAccount),
    INDEX IX_BlockHeight (BlockHeight)
);
⚡ Performance
Benchmarks (Mainnet)
Metric	Value
Indexing Speed	~50 blocks/second
API Response Time	<50ms (p95)
Database Size	~500 GB (full history)
Memory Usage	~2 GB
CPU Usage	~15% (4 cores)
Optimization Tips
Enable Database Indexes - Ensure all key indexes are created

Use Caching - Redis/MemoryCache for frequent queries

Batch Processing - Process blocks in batches of 100-500

Connection Pooling - Configure connection pool size appropriately

Async Operations - Use async/await throughout

🤝 Contributing
Contributions are welcome! Please follow these steps:

Fork the repository

Create a feature branch

Commit your changes

Push to the branch

Open a Pull Request

Development Guidelines

Follow C# coding conventions

Write unit tests for new features

Update API documentation in Swagger

Keep commit messages clear and descriptive

📝 License
This project is licensed under the MIT License - see the LICENSE file for details.

👤 Author
Kamil Szymoniak

GitHub: @midware

Project: node_casper_parser

🙏 Acknowledgments
Casper Network Team - For the excellent blockchain infrastructure

Casper .NET SDK - Reference implementation and examples

Community Contributors - For feedback and improvements

📞 Support
Issues: GitHub Issues

Mainnet API: https://api.node.casper.mystra.io/swagger

Testnet API: https://api.testnet.node.casper.io/swagger

⭐ If this project helped you, leave a star! ⭐

Made with ❤️ for Casper Network Community