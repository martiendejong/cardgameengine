# Card Game Engine

A generic, data-driven Trading Card Game engine. Game rules are defined in JSON and executed by a generic rule engine. The first game to run on it is **Town Wars** (Town TCG).

## Architecture

```
src/
  CardGameEngine.Core/     - Domain models (definition + runtime layer)
  CardGameEngine.Engine/   - Rule engine, effect processor, stack manager
  CardGameEngine.Api/      - ASP.NET Core 8 Web API + SignalR hub
frontend/                  - React 18 + TypeScript + Vite
definitions/
  town-tcg/
    game.json              - Complete Town TCG definition
```

## Running

### Backend

```bash
cd src/CardGameEngine.Api
dotnet run
```

API runs on `http://localhost:5001`.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on `http://localhost:5173` and proxies `/api` and `/gamehub` to the backend.

## How it works

- Game rules (card definitions, phases, effects, costs) are fully defined in `definitions/town-tcg/game.json`
- The rule engine interprets those definitions at runtime
- SignalR broadcasts game state updates to all connected clients after each action
- No database needed: all state is in memory

## Town Wars

Two players each start with a Town Hall (HQ) and a Town Chief (Hero). The goal is to destroy all of the opponent's headquarters and heroes.

- **Town Hall**: Can tap to collect 2 gold, or spend 2 gold + tap to summon a Peasant
- **Town Chief**: A 4/5 hero with 2 armor. Can spend 3 AP to buff all Peasants, or spend 2 AP + tap to heal 3 HP
- **Peasant**: A 2/3 unit with 1 armor and the Peasant/Worker/Builder tags

Phases each turn: Start (untap + gain hero AP) -> Main -> Combat -> End
