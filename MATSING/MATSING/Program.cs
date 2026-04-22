/*
 * ═══════════════════════════════════════════════════════════════════════════
 *  MATSING — Matching Card Game
 *  A C# Windows Forms Application
 * ═══════════════════════════════════════════════════════════════════════════
 *
 *  AOOP PRINCIPLES MAP
 *  ───────────────────────────────────────────────────────────────────────
 *
 *  1. ABSTRACTION
 *     ├── Models/IFlippable.cs      — interface: contract for flippable cards
 *     ├── Models/IScoreable.cs      — interface: contract for scoreable cards
 *     └── Models/CardBase.cs        — abstract class: shared card template;
 *                                     Reveal() and CalculateBonus() are abstract
 *
 *  2. ENCAPSULATION
 *     ├── Game/GameEngine.cs        — ALL game state (deck, flip pair,
 *     │                               board lock, move count) is PRIVATE;
 *     │                               exposed only via public events + API
 *     ├── Game/ScoreTracker.cs      — hidden scoring formula, streak counter;
 *     │                               only Score property is publicly readable
 *     └── Game/GameTimer.cs         — wraps WinForms Timer; tick wiring is
 *                                     fully private
 *
 *  3. INHERITANCE
 *     ├── Models/MonkeyCard  : CardBase      (domain model)
 *     ├── Models/SpecialCard : CardBase      (domain model, bonus variant)
 *     ├── Controls/CardControl   : Panel     (WinForms UI base)
 *     ├── Controls/MonkeyCardControl : CardControl
 *     └── Controls/SpecialCardControl : CardControl
 *
 *  4. POLYMORPHISM
 *     GameForm holds List<CardControl> — at runtime elements are
 *     MonkeyCardControl OR SpecialCardControl.
 *     Calling  control.PlayFlipAnimation()   or
 *              control.PlayMatchAnimation()
 *     on the base reference dispatches to the CORRECT subclass override
 *     automatically — no if/switch needed in GameForm.
 *
 *     MonkeyCardControl.PlayFlipAnimation()  → bounce flip
 *     SpecialCardControl.PlayFlipAnimation() → flash flip  (different!)
 *     MonkeyCardControl.PlayMatchAnimation() → gold glow
 *     SpecialCardControl.PlayMatchAnimation()→ teal shimmer (different!)
 *
 * ═══════════════════════════════════════════════════════════════════════════
 */

using MATSING.Forms;

// Enable high-DPI awareness and visual styles
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetHighDpiMode(HighDpiMode.SystemAware);

// Launch the main menu
Application.Run(new MainMenuForm());
