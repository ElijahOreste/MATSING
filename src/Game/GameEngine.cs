using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Game;

// ═══════════════════════════════════════════════════════════════
//  AOOP PRINCIPLE #2 — ENCAPSULATION  (primary showcase)
//  ALL game state lives here as private fields.
//  The outside world cannot touch _deck, _firstFlipped,
//  _boardLocked, etc. directly — it can only call the public API
//  and listen to the public Events.
//
//  AOOP PRINCIPLE #1 — ABSTRACTION (usage)
//  GameEngine works entirely with CardBase references —
//  it never cares whether the card is MonkeyCard or SpecialCard.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// <b>ENCAPSULATION</b> — GameEngine<br/>
/// The single source of truth for all in-progress game state.
/// <list type="bullet">
///   <item><description>All mutable state fields are <c>private</c>.</description></item>
///   <item><description>External code interacts ONLY via the public method API and C# events.</description></item>
///   <item><description>No external class can corrupt the board-lock, match count, or flip pair.</description></item>
/// </list>
/// </summary>
public class GameEngine
{
    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE STATE — the encapsulated core
    // ═══════════════════════════════════════════════════════════════
    private List<CardBase>  _deck        = new();
    private CardBase?       _firstFlipped;
    private CardBase?       _secondFlipped;
    private int             _matchCount;
    private int             _moveCount;
    private bool            _boardLocked;
    private Difficulty      _difficulty;
    private DifficultyConfig _config      = DifficultyConfig.For(Difficulty.Easy);

    // ── Collaborators (also private) ──────────────────────────────────────
    private readonly ScoreTracker _score  = new();

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC EVENTS — the only window into game state changes
    // ═══════════════════════════════════════════════════════════════
    /// <summary>Fired when a matching pair is found.</summary>
    public event EventHandler<CardPairEventArgs>? MatchFound;

    /// <summary>Fired when two non-matching cards are flipped.</summary>
    public event EventHandler<CardPairEventArgs>? MismatchFound;

    /// <summary>Fired when all pairs have been matched (game complete).</summary>
    public event EventHandler? GameWon;

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC READ-ONLY PROPERTIES — safe views of private state
    // ═══════════════════════════════════════════════════════════════
    /// <summary>Number of matched pairs so far.</summary>
    public int MatchCount  => _matchCount;

    /// <summary>Total moves (pairs of flips) made by the player.</summary>
    public int MoveCount   => _moveCount;

    /// <summary>Current player score managed by <see cref="ScoreTracker"/>.</summary>
    public int Score       => _score.Score;

    /// <summary>Current consecutive-match streak.</summary>
    public int Streak      => _score.Streak;

    /// <summary>Returns true when all pairs have been matched.</summary>
    public bool IsComplete => _matchCount == _deck.Count / 2;

    /// <summary>The active difficulty configuration.</summary>
    public DifficultyConfig Config => _config;

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialises a new game: builds the master deck, selects pairs
    /// based on difficulty, shuffles, and resets all private state.
    /// </summary>
    /// <param name="difficulty">Selected difficulty level.</param>
    /// <param name="elapsedSeconds">Starts at 0 — ignored here, passed in per flip.</param>
    public void StartNewGame(Difficulty difficulty)
    {
        _difficulty   = difficulty;
        _config       = DifficultyConfig.For(difficulty);
        _matchCount   = 0;
        _moveCount    = 0;
        _boardLocked  = false;
        _firstFlipped = null;
        _secondFlipped= null;
        _score.Reset();

        // Build master deck of all 9 unique monkey cards
        var master = BuildMasterDeck();

        // Pick only as many pairs as the difficulty requires
        var selected = ShuffleHelper.Shuffle(master).Take(_config.PairCount).ToList();

        // Duplicate each card to form pairs, then shuffle the full set
        var pairs = ShuffleHelper.Shuffle(
            selected.SelectMany(c => new[] { c, CloneCard(c) }).ToList());

        _deck = pairs;
    }

    /// <summary>
    /// Returns a read-only view of the current deck.
    /// Callers cannot modify the internal list.
    /// </summary>
    public IReadOnlyList<CardBase> GetDeck() => _deck.AsReadOnly();

    /// <summary>
    /// Called when the player clicks a card.
    /// Validates the click (locked board, already matched, same card)
    /// and manages the first/second flip logic internally.
    /// </summary>
    /// <param name="card">The card that was selected.</param>
    /// <param name="elapsedSeconds">Time elapsed for bonus calculation.</param>
    /// <returns>True if the flip was accepted; false if ignored.</returns>
    public bool OnCardSelected(CardBase card, int elapsedSeconds)
    {
        if (_boardLocked)            return false;
        if (card.IsMatched)          return false;
        if (card.IsFaceUp)           return false;
        if (card == _firstFlipped)   return false;

        card.FlipUp();
        card.Reveal();

        if (_firstFlipped == null)
        {
            _firstFlipped = card;
            return true;
        }

        // Second card selected — evaluate match
        _secondFlipped = card;
        _moveCount++;
        _boardLocked = true;       // lock board until result is processed

        if (_firstFlipped.CardId == _secondFlipped.CardId)
        {
            // ── Match ──────────────────────────────────────────────────────
            _firstFlipped.MarkAsMatched();
            _secondFlipped.MarkAsMatched();
            _matchCount++;
            _score.RegisterMatch(elapsedSeconds, _firstFlipped.PointValue);

            var args = new CardPairEventArgs(_firstFlipped, _secondFlipped);
            MatchFound?.Invoke(this, args);

            ResetFlipPair();
            _boardLocked = false;

            if (IsComplete) GameWon?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // ── Mismatch ───────────────────────────────────────────────────
            _score.RegisterMismatch();
            MismatchFound?.Invoke(this,
                new CardPairEventArgs(_firstFlipped, _secondFlipped));
            // Board remains locked; caller must call FlipDownMismatch() after delay
        }

        return true;
    }

    /// <summary>
    /// Flips the mismatched pair back down and unlocks the board.
    /// Call this after the "show mismatch" delay in the UI.
    /// </summary>
    public void FlipDownMismatch()
    {
        _firstFlipped?.FlipDown();
        _secondFlipped?.FlipDown();
        ResetFlipPair();
        _boardLocked = false;
    }

    // ── Private Helpers ───────────────────────────────────────────────────
    private void ResetFlipPair()
    {
        _firstFlipped  = null;
        _secondFlipped = null;
    }

    /// <summary>Builds the full 9-card master deck from known monkey assets.</summary>
    private static List<CardBase> BuildMasterDeck() => new()
    {
        new MonkeyCard(1, "Serenade", @"Assets\Cards\download__7_.jpg",          "Troubadour",    "Romantic"),
        new MonkeyCard(2, "CEO Mode", @"Assets\Cards\download__2_.jpg",           "Executive",     "Serious"),
        new MonkeyCard(3, "Scholar",  @"Assets\Cards\meme_macaco.jpg",            "Scribbles",     "Intellectual"),
        new MonkeyCard(4, "Shocked",  @"Assets\Cards\download__4_.jpg",           "Gaspito",       "Dramatic"),
        new MonkeyCard(5, "Ottoman",  @"Assets\Cards\turkish_monkey__thumb_up__reaction_pic.jpg", "Mehmet Bey", "Authoritative"),
        new MonkeyCard(6, "Romeo",    @"Assets\Cards\Instagram_fahimvine.jpg",    "Señor Flower",  "Charming"),
        new MonkeyCard(7, "Cutie",    @"Assets\Cards\download__5_.jpg",           "Rosie",         "Sweet"),
        new MonkeyCard(8, "Thinker",  @"Assets\Cards\download__6_.jpg",           "Philosopher",   "Contemplative"),
        new MonkeyCard(9, "Astro",    @"Assets\Cards\monyet_astronot.jpg",        "Commander Bak", "Adventurous"),
    };

    /// <summary>Creates a fresh copy of a card with the same ID (for pair building).</summary>
    private static CardBase CloneCard(CardBase source) => source switch
    {
        MonkeyCard m => new MonkeyCard(m.CardId, m.Label, m.ImagePath, m.CharacterName, m.Personality),
        SpecialCard s => new SpecialCard(s.CardId, s.Label, s.ImagePath, s.SpecialAbility),
        _ => throw new InvalidOperationException("Unknown card type")
    };
}

// ── Event Args ────────────────────────────────────────────────────────────────

/// <summary>Event data carrying a pair of cards involved in a match/mismatch.</summary>
public class CardPairEventArgs : EventArgs
{
    public CardBase First  { get; }
    public CardBase Second { get; }
    public CardPairEventArgs(CardBase first, CardBase second) { First = first; Second = second; }
}
