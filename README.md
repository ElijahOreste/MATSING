# 🐾 MATSING – Matching Card Game

A **C# Windows Forms Application** built with **.NET 6** that demonstrates all four pillars of **Advanced Object-Oriented Programming (AOOP)** through a fully playable monkey-themed card matching game.

---

## 🚀 How to Run

### Prerequisites
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (Windows)
- Visual Studio 2022 **or** any terminal with `dotnet` CLI

### Run from terminal
```bash
cd MATSING
dotnet run --project MATSING/MATSING.csproj
```

### Run from Visual Studio
1. Open `MATSING.sln`
2. Press **F5** (or click ▶ Run)

> **Note:** The 9 card images (`Assets/Cards/*.jpg`) are copied automatically to the output directory on build.

---

## 🎮 How to Play

1. Select a difficulty on the **Main Menu**: Easy (8 cards), Medium (12), Hard (18)
2. Click **PLAY**
3. Click any two cards to flip them — find the matching monkey pair
4. Match all pairs before the countdown reaches zero to win!
5. Combos (consecutive matches) multiply your score

---

## 🧱 AOOP Principles — Where to Find Them

### 1. 🔷 ABSTRACTION
> *"Hide complexity; expose only what is necessary."*

| File | What it abstracts |
|------|-------------------|
| `Models/IFlippable.cs` | Contract for any flippable card — callers call `FlipUp()` without knowing the implementation |
| `Models/IScoreable.cs` | Contract for any scoreable entity — `CalculateBonus()` hides the formula |
| `Models/CardBase.cs` | Abstract class: `Reveal()` and `CalculateBonus()` are **abstract** — subclasses must supply them |

### 2. 🔶 ENCAPSULATION
> *"Bundle data with the methods that operate on it; hide internal state."*

| File | What is encapsulated |
|------|----------------------|
| `Game/GameEngine.cs` | `_deck`, `_firstFlipped`, `_boardLocked`, `_moveCount` — all `private`. External code uses events + read-only properties only |
| `Game/ScoreTracker.cs` | `_streak`, `_baseScore`, scoring constants — all `private`. Only `Score` is publicly readable |
| `Game/GameTimer.cs` | Wraps `System.Windows.Forms.Timer`; tick wiring, interval, and elapsed tracking are fully hidden |

### 3. 🔺 INHERITANCE
> *"Derive new classes from existing ones, reusing and extending behaviour."*

```
CardBase (abstract)
├── MonkeyCard      ← inherits flip/match state; adds CharacterName, Personality
└── SpecialCard     ← inherits flip/match state; adds SpecialAbility; PointValue = 200

Panel (WinForms)
└── CardControl     ← inherits all Panel layout/paint; adds card painting + virtual animations
    ├── MonkeyCardControl   ← adds bounce, character name badge
    └── SpecialCardControl  ← adds flash flip, ⭐ SPECIAL badge
```

### 4. 🔵 POLYMORPHISM
> *"One interface, many forms — same call, different behaviour per subtype."*

**In `GameForm.cs`:**
```csharp
// GameForm holds a List<CardControl> — concrete types invisible here
List<CardControl> _cardControls = new();

// ONE call — CLR dispatches to the correct override at RUNTIME:
ctrl.PlayFlipAnimation();
//  MonkeyCardControl  → bounce + horizontal squish flip
//  SpecialCardControl → horizontal squish flip + white flash

ctrl.PlayMatchAnimation();
//  MonkeyCardControl  → gold glow pulse
//  SpecialCardControl → teal shimmer flash
```

No `if/switch` statement is needed — the CLR handles dispatch automatically.

---

## 📁 Project Structure

```
MATSING/
├── MATSING.sln
└── MATSING/
    ├── Program.cs              ← Entry point + AOOP map comment
    ├── MATSING.csproj
    ├── Models/
    │   ├── IFlippable.cs       ← ABSTRACTION (interface)
    │   ├── IScoreable.cs       ← ABSTRACTION (interface)
    │   ├── CardBase.cs         ← ABSTRACTION (abstract class)
    │   ├── MonkeyCard.cs       ← INHERITANCE
    │   ├── SpecialCard.cs      ← INHERITANCE
    │   └── Difficulty.cs       ← Enum
    ├── Game/
    │   ├── GameEngine.cs       ← ENCAPSULATION (primary)
    │   ├── ScoreTracker.cs     ← ENCAPSULATION
    │   ├── GameTimer.cs        ← ENCAPSULATION
    │   └── DifficultyConfig.cs
    ├── Controls/
    │   ├── CardControl.cs      ← INHERITANCE (: Panel) + POLYMORPHISM (virtual)
    │   ├── MonkeyCardControl.cs← POLYMORPHISM (override)
    │   ├── SpecialCardControl.cs← POLYMORPHISM (override)
    │   └── ScorePillControl.cs ← INHERITANCE (: Panel)
    ├── Forms/
    │   ├── MainMenuForm.cs     ← Main menu UI
    │   ├── GameForm.cs         ← Game board + POLYMORPHISM in action
    │   └── WinForm.cs          ← Results dialog
    ├── Assets/Cards/           ← 9 monkey card images
    └── Utils/
        ├── ShuffleHelper.cs    ← Fisher-Yates shuffle
        └── AnimationHelper.cs  ← GDI+ animation helpers
```

---

## 🎨 UI/UX Design

Inspired by the `matsing.html` reference design:

| Token | Value |
|-------|-------|
| Background | `#1A0A2E` deep space purple |
| Panel BG | `#2B1050` |
| Accent Gold | `#F7C948` |
| Accent Red | `#FF6B6B` |
| Accent Teal | `#5EFFD1` |
| Font | Segoe UI Black (titles), Segoe UI (body) |

All custom controls use `DoubleBuffered = true` and GDI+ `SmoothingMode.AntiAlias` for smooth rendering. Animations are driven by `System.Windows.Forms.Timer` — no external libraries required.

---

## 🃏 Card Roster

| ID | File | Label | Character | Personality |
|----|------|-------|-----------|-------------|
| 1 | download__7_.jpg | Serenade | Troubadour | Romantic |
| 2 | download__2_.jpg | CEO Mode | Executive | Serious |
| 3 | meme_macaco.jpg | Scholar | Scribbles | Intellectual |
| 4 | download__4_.jpg | Shocked | Gaspito | Dramatic |
| 5 | turkish_monkey…jpg | Ottoman | Mehmet Bey | Authoritative |
| 6 | Instagram_fahimvine.jpg | Romeo | Señor Flower | Charming |
| 7 | download__5_.jpg | Cutie | Rosie | Sweet |
| 8 | download__6_.jpg | Thinker | Philosopher | Contemplative |
| 9 | monyet_astronot.jpg | Astro | Commander Bak | Adventurous |
