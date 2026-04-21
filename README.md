# Fantasy Bazaar

### *A Real-Time Dynamic Pricing Marketplace Simulation*

---

<img width="2556" height="1401" alt="image" src="https://github.com/user-attachments/assets/3bf2a524-7b70-4e3c-946b-284376068cff" />

<img width="2558" height="1387" alt="image" src="https://github.com/user-attachments/assets/fe5a1788-536c-403f-b07b-08de73aebf6c" />

## Highlights

- **Dynamic Pricing Engine** – Prices change automatically based on stock levels, popularity scores, and random market events
- **Autonomous NPC Shoppers** – Multiple NPCs buy items simultaneously, testing concurrency handling under real load
- **Real-Time Updates** – SignalR pushes stock and price changes instantly to all connected clients
- **Concurrency Safety** – Database row locks prevent overselling, even under heavy contention
- **Containerized** – Run everything with a docker compose command

---

## Overview

Fantasy Bazaar is a simulated marketplace where prices respond to supply and demand in real time. Built as a portfolio project to demonstrate distributed systems patterns, it combines:

- Background services for pricing and inventory replenishment
- Real-time client updates via SignalR
- Concurrency-safe purchasing with database transactions
- Autonomous NPCs that create realistic load

The system runs completely in Docker and requires no external dependencies beyond Docker itself.

### What This Project Demonstrates

| Concept | Implementation |
|---------|----------------|
| Background processing | IHostedServices for pricing engine, NPCs, and replenishment |
| Real-time communication | SignalR hub broadcasting stock/price updates |
| Concurrency | Database row locks + execution strategies |
| Caching & distribution | Redis for inventory caching with graceful degradation |
| System simulation | NPC agents using the same purchase pipeline |

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10, C# |
| Real-time | SignalR |
| Database | PostgreSQL |
| Caching | Redis |
| Container | Docker + Docker Compose |
| Frontend | HTML/CSS/JavaScript |
| ORM | Entity Framework Core |

---

## How to Run

### Requirements

- Docker Desktop (or Docker + Docker Compose)

### One-Command Setup

```bash
git clone https://github.com/totaleey/FantasyBazaar.git
cd FantasyBazaar
docker-compose up --build
