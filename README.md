# ✨ Memory Match — Enhanced Matching Game

A modern, polished C# Windows Forms memory card-matching game with dark UI, animations, difficulty levels, score tracking, and combo system.

---

## 📋 Project Description and Purpose

**Memory Match** is an enhanced version of the classic Microsoft Matching Game tutorial. The player flips cards to find matching pairs of emoji symbols. The goal is to match all pairs in as few moves as possible (and before time runs out on harder difficulties).

The project was built to demonstrate all four Object-Oriented Programming (OOP) principles — **Encapsulation, Inheritance, Polymorphism, and Abstraction** — within a real, playable Windows Forms application.

---

## 🗂 Project Structure

```
MatchingGame/
├── Program.cs                        Entry point
├── MainForm.cs                       Main game window
├── GameResultForm.cs                 Win/Lose result screen with confetti
├── Models/
│   ├── CardBase.cs                   Abstract base card (Abstraction)
│   ├── IconCard.cs                   Concrete card (Inheritance + Polymorphism)
│   ├── DifficultySettings.cs         Difficulty config (Encapsulation)
│   └── GameState.cs                  Score + state tracking (Encapsulation)
├── Controllers/
│   └── GameController.cs             Game logic (implements IGameController)
├── Interfaces/
│   └── IGameController.cs            Game contract (Abstraction via Interface)
└── UI/
    └── CardButton.cs                 Custom card control (Inheritance from Button)
```

---

## 🧱 UML Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        «interface»                              │
│                      IGameController                            │
│  + State: GameState                                             │
│  + Settings: DifficultySettings                                 │
│  + Board: List<List<CardBase>>                                  │
│  + StartNewGame(difficulty: Difficulty)                         │
│  + RevealCard(row, col): bool                                   │
│  + ResetGame()                                                  │
│  + «event» MatchFound                                           │
│  + «event» MatchMissed                                          │
│  + «event» GameCompleted                                        │
└──────────────────────┬──────────────────────────────────────────┘
                       │ implements
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                     GameController                              │
│  - _firstCard: CardBase                                         │
│  - _secondCard: CardBase                                        │
│  - _state: GameState                                            │
│  - _settings: DifficultySettings                                │
│  + HideMismatchedCards()                                        │
└──────────────────────┬──────────────────────────────────────────┘
                       │ uses
          ┌────────────┼────────────┐
          ▼            ▼            ▼
  ┌──────────────┐ ┌──────────┐ ┌──────────────────┐
  │  GameState   │ │Difficulty│ │  DifficultySettings│
  │  - _moves    │ │Settings  │ │  + Level           │
  │  - _score    │ │  Easy    │ │  + GridSize        │
  │  - _matches  │ │  Medium  │ │  + TimeLimit       │
  │  + RecordMove│ │  Hard    │ │  + Create()        │
  │  + RecordMatch│ └─────────┘ └──────────────────┘
  └──────────────┘

         «abstract»
┌───────────────────────────────┐
│          CardBase             │
│  # _isRevealed: bool          │
│  # _isMatched: bool           │
│  # _symbol: string            │
│  + «abstract» Reveal()        │
│  + «abstract» Hide()          │
│  + «abstract» MarkMatched()   │
│  + «abstract» MatchesWith()   │
│  + «virtual» GetDisplayText() │
└──────────────┬────────────────┘
               │ inherits
               ▼
┌──────────────────────────────┐
│         IconCard             │
│  + Reveal() override         │
│  + Hide() override           │
│  + MarkMatched() override    │
│  + MatchesWith() override    │
│  + GetDisplayText() override │
└──────────────────────────────┘

     System.Windows.Forms.Button
               │ inherits
               ▼
┌──────────────────────────────┐
│         CardButton           │
│  - _card: CardBase           │
│  - _flipProgress: float      │
│  - _animTimer: Timer         │
│  + AnimateFlip(reveal: bool) │
│  # OnPaint() override        │
└──────────────────────────────┘

┌──────────────────────────────┐
│          MainForm            │
│  - _controller: GameController│
│  - _cardButtons[,]: CardButton│
│  + StartNewGame()            │
│  + BuildBoard()              │
└──────────────────────────────┘
```

---

## ✨ Features and Functionalities

| Feature | Description |
|---------|-------------|
| **3 Difficulty Levels** | Easy (4×4, no timer), Medium (6×6, 120s), Hard (6×6, 60s) |
| **Emoji Card Symbols** | 32 unique emoji icons, randomly shuffled each game |
| **Flip Animations** | Smooth CSS-style flip animation on each card |
| **Combo System** | Consecutive matches trigger a 🔥 combo counter |
| **Score Tracking** | Points awarded per match; bonuses for time and efficiency |
| **Countdown Timer** | Visible timer for Medium/Hard; turns red below 10s |
| **Win/Lose Screen** | Animated confetti on win; shows score, moves, and time |
| **Responsive Board** | Board resizes to fit any window size |
| **Dark Modern UI** | Custom-painted controls with gradient backgrounds |
| **Custom CardButton** | Fully owner-drawn card control with rounded corners |

---

## ⚙️ How the Program Works

1. **Startup**: `Program.cs` launches `MainForm`, which creates a `GameController`.
2. **New Game**: The player picks a difficulty and clicks "New Game". `GameController.StartNewGame()` generates a shuffled grid of `IconCard` objects.
3. **Card Click**: Clicking a `CardButton` calls `GameController.RevealCard(row, col)`. The card animates a flip to reveal its emoji.
4. **Matching Logic**:
   - First click → stores `_firstCard`
   - Second click → compares with `_firstCard.MatchesWith(secondCard)`
   - **Match**: both cards call `MarkMatched()` → green glow; score increases
   - **No Match**: a short timer fires `HideMismatchedCards()` → cards flip back; score deducted
5. **Events**: `MatchFound`, `MatchMissed`, and `GameCompleted` events allow `MainForm` to update the UI without coupling to game logic.
6. **Game End**: When all pairs are matched (or time runs out), `GameResultForm` displays results with confetti on victory.

---

## 🧩 OOP Principles Applied

### 1. Encapsulation
- `CardBase` uses private backing fields with public read-only properties (`IsRevealed`, `IsMatched`, `Symbol`).
- `GameState` hides `_moves`, `_score`, `_matchesFound` from outside — only exposing them through read-only properties and controlled mutator methods (`RecordMove()`, `RecordMatch()`).
- `DifficultySettings` has a private constructor; state is only set through the `Create()` factory method.

### 2. Inheritance
- `IconCard` inherits from the abstract `CardBase` class.
- `CardButton` inherits from `System.Windows.Forms.Button`, extending it with card rendering and flip animations.

### 3. Polymorphism
- `CardBase` declares abstract methods (`Reveal`, `Hide`, `MarkMatched`, `MatchesWith`, `GetDisplayText`) that `IconCard` overrides with specific behavior.
- `CardButton.OnPaint()` overrides the base `Button.OnPaint()` to provide fully custom rendering.
- `DifficultySettings.Create()` is a factory method that returns different configurations polymorphically based on the `Difficulty` enum.

### 4. Abstraction
- `CardBase` is an abstract class that defines what a card *must* do, without specifying how.
- `IGameController` is an interface that defines the contract for any game controller, decoupling `MainForm` from the concrete `GameController` implementation.

---

## 🚀 How to Run the Application

### Requirements
- Windows 10 or 11
- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) (or .NET 8 SDK)
- Visual Studio 2022 (recommended) **or** VS Code with C# extension

### Steps

**Using Visual Studio 2022:**
1. Clone or download this repository
2. Open `MatchingGame.sln` or `MatchingGame.csproj` in Visual Studio
3. Press `F5` or click **Run** to build and launch

**Using .NET CLI:**
```bash
git clone <your-repo-url>
cd MatchingGame
dotnet run
```

**Building a release executable:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```
The `.exe` will be in `bin/Release/net6.0-windows/win-x64/publish/`.

---

## 👥 Team Members

| Name | Role |
|------|------|
| Elijah | Game Logic / OOP Architecture |
| Justine | UI Design / Custom Controls |
| Zaireh | Score System / Difficulty Modes |
| Daniel | Testing / Documentation |

---

## 📸 Screenshots

> _(Add screenshots of gameplay here)_

---

## 📄 License

This project is for educational purposes as a final project submission for Advanced OOP.
