# Vortiq 🚀

> **A Web3 arcade shooter built on Solana** — destroy asteroids, earn rewards, and trade tokens without leaving the game.

**Unity 6 (6000.2.8f1) · Solana Mainnet · Android & WebGL**

---

## 🎮 Game Overview

Vortiq is a fast-paced asteroid shooter with progressive difficulty. Destroy incoming asteroids to score points, survive as long as possible, and compete on the global leaderboard — all linked to your Solana wallet.

### Gameplay

| Feature | Details |
|---------|---------|
| **Core Loop** | Shoot asteroids before they hit you. Large asteroids split into smaller ones. |
| **Difficulty Scaling** | Spawn rate and wave size increase continuously over time |
| **Lives** | 3 lives — respawn on death, game over when all are lost |
| **Scoring** | Points per asteroid destroyed; high score tracked globally |

### Power-Ups

| Power-Up | Effect |
|----------|--------|
| 🛡️ Shield | Blocks one hit |
| 🔫 Multiple Bullets | Expands fire spread |
| ❄️ Freeze Time | Stops all asteroids momentarily |
| ❤️ Full Lives | Restores all 3 lives |

Power-ups are purchased from the in-game shop using SOL or `$PLAY` tokens and activated mid-game.

---

## ⛓️ Blockchain & Web3 Features

### 1. Solana Wallet Connection

Players connect their Solana wallet to authenticate and access all Web3 features.

- **Mobile**: Connected via the [MagicBlock](https://magicblock.gg) SDK — supports Phantom, Jupiter, and other mobile wallets
- **Editor / Desktop**: Solana Unity SDK keypair (BIP39 mnemonic)
- Wallet address is displayed in-game and can be copied to clipboard
- Username linked to wallet address, stored via Unity Authentication

**Script**: `WalletConnector.cs`

---

### 2. In-Game Token Marketplace

Players can purchase power-ups with either SOL or SPL tokens (`$PLAY`).

- Checks wallet balance before confirming purchase
- Builds and signs Solana transactions natively in-game
- Handles ATA (Associated Token Account) creation automatically if needed
- Platform-aware signing: direct keypair in Editor, wallet adapter on Android

**Script**: `MarketplacePurchase.cs`

---

### 3. Jupiter DEX Swap (In-Game)

A full token swap UI is embedded in the game, powered by the [Jupiter Aggregator](https://jup.ag).

- Minimum swap amount: **0.003 SOL**
- Live price quotes fetched from Jupiter's API with retry logic
- Supports SOL, USDC, and custom SPL tokens
- Displays price impact, fees, and route information before confirming
- Executes swap and signs the transaction from within the game
- Real-time balance display for all supported tokens

**Script**: `JupiterSwapManager.cs`

---

### 4. Leaderboard System

Scores are submitted to the Unity Services cloud leaderboard:

- Authenticated via Unity Gaming Services
- Wallet address used as the anonymous player ID
- Username customizable in-game and synced to Unity Auth
- Persists across sessions; viewable globally in-game

**Scripts**: `DualLeaderboardManager.cs`, `LeaderboardDisplay.cs`

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Unity 6 (6000.2.8f1) |
| Blockchain | Solana (Mainnet) |
| Wallet SDK | [Solana Unity SDK](https://github.com/magicblock-labs/Solana.Unity-SDK) |
| Mobile Wallet | [MagicBlock](https://magicblock.gg) SDK |
| DEX | [Jupiter Aggregator v6](https://jup.ag) |
| Cloud Services | Unity Gaming Services (Auth + Leaderboards) |
| Tweening | DOTween |
| Platform | Android, WebGL |

---

## 📁 Project Structure

```
Assets/Scripts/
├── Ini/                         # Core game logic
│   ├── GameManager.cs           # Game state, scoring, lives
│   ├── Player.cs                # Ship movement, shooting, upgrades
│   ├── Asteroid.cs              # Asteroid physics & split logic
│   ├── AsteroidSpawner.cs       # Difficulty scaling & spawn management
│   ├── AsteroidPool.cs          # Object pooling (no GC spikes)
│   ├── PowerUpManager.cs        # Power-up activation & timers
│   └── UIManager.cs             # HUD and menu management
│
└── Ragna Ebod/
    ├── Analytics/               # Leaderboard & user identity
    │   ├── DualLeaderboardManager.cs
    │   ├── LeaderboardDisplay.cs
    │   └── UsernameSetupPanel.cs
    │
    └── Blockchain/
        ├── Wallet/              # Wallet connection & marketplace
        │   ├── WalletConnector.cs
        │   └── MarketplacePurchase.cs
        ├── JupiterSwaps/        # In-game DEX swap
        │   └── JupiterSwapManager.cs
        └── Ephemeral Rollups/   # On-chain randomness
            ├── MagicRollupManager.cs
            └── VortiqProgram.cs
```

## 📄 License

This project is proprietary. All rights reserved.
