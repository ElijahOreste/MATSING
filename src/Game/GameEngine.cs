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
    private List<CardBase>   _deck          = new();
    private CardBase?        _firstFlipped;
    private CardBase?        _secondFlipped;
    private CardBase?        _thirdFlipped;      // TripleMatch support
    private int              _matchCount;
    private int              _moveCount;
    private bool             _boardLocked;
    private Difficulty       _difficulty;
    private DifficultyConfig _config        = DifficultyConfig.For(Difficulty.Easy);
    private GameModifier     _modifiers     = GameModifier.None;

    // FlipLimit state — tracks per-card remaining flips
    private Dictionary<CardBase, int> _flipLimits = new();
    private const int FLIP_LIMIT_MAX = 2;

    // ── Collaborators (also private) ──────────────────────────────────────
    private readonly ScoreTracker _score = new();

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC EVENTS — the only window into game state changes
    // ═══════════════════════════════════════════════════════════════
    /// <summary>Fired when a matching pair (or triple) is found.</summary>
    public event EventHandler<CardPairEventArgs>? MatchFound;

    /// <summary>Fired when the flipped cards don't match.</summary>
    public event EventHandler<CardPairEventArgs>? MismatchFound;

    /// <summary>Fired when all pairs have been matched (game complete).</summary>
    public event EventHandler? GameWon;

    /// <summary>Fired when Hardcore Mode ends the game on a mismatch.</summary>
    public event EventHandler? GameOverHardcore;

    /// <summary>Fired when CardDrift swaps card positions (carries the shuffled deck order).</summary>
    public event EventHandler<DriftEventArgs>? CardsDrifted;

    /// <summary>Fired each second with the current ShrinkingCards scale factor.</summary>
    // ShrinkTick removed — shrinking is driven by GameForm's own timer, not the engine

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
    public bool IsComplete => _matchCount == TotalPairs;

    /// <summary>The active difficulty configuration.</summary>
    public DifficultyConfig Config => _config;

    /// <summary>Active game modifiers.</summary>
    public GameModifier Modifiers => _modifiers;

    /// <summary>True when ZenMode is active (no timer).</summary>
    public bool IsZenMode => _modifiers.HasFlag(GameModifier.ZenMode);

    /// <summary>Total number of pairs in the current game (varies with TripleMatch).</summary>
    public int TotalPairs => _modifiers.HasFlag(GameModifier.TripleMatch)
        ? _deck.Count / 3
        : _deck.Count / 2;

    /// <summary>Number of cards required per match (2 normally, 3 with TripleMatch).</summary>
    public int MatchGroupSize => _modifiers.HasFlag(GameModifier.TripleMatch) ? 3 : 2;

    /// <summary>Remaining flips for a given card, or -1 if FlipLimit not active.</summary>
    public int FlipsRemaining(CardBase card) =>
        _modifiers.HasFlag(GameModifier.FlipLimit) && _flipLimits.TryGetValue(card, out int r) ? r : -1;

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialises a new game: builds the master deck, selects pairs
    /// based on difficulty, shuffles, and resets all private state.
    /// </summary>
    public void StartNewGame(Difficulty difficulty, GameModifier modifiers = GameModifier.None)
    {
        _difficulty   = difficulty;
        _modifiers    = modifiers;
        _config       = DifficultyConfig.For(difficulty);
        _matchCount   = 0;
        _moveCount    = 0;
        _boardLocked  = false;
        _firstFlipped = null;
        _secondFlipped= null;
        _thirdFlipped = null;
        _score.Reset(_modifiers.HasFlag(GameModifier.ComboMultiplier));
        _flipLimits.Clear();

        // Build master deck of all 9 unique monkey cards
        var master = BuildMasterDeck();

        // Pick as many unique cards as needed, then duplicate for pairs/triples
        int groupSize = _modifiers.HasFlag(GameModifier.TripleMatch) ? 3 : 2;
        var selected  = ShuffleHelper.Shuffle(master).Take(_config.PairCount).ToList();

        // Create groups of 2 or 3 per card
        var groups = selected.SelectMany(c =>
        {
            var group = new List<CardBase> { c };
            for (int i = 1; i < groupSize; i++) group.Add(CloneCard(c));
            return group;
        }).ToList();

        _deck = ShuffleHelper.Shuffle(groups);

        // Initialise FlipLimit counters
        if (_modifiers.HasFlag(GameModifier.FlipLimit))
        {
            foreach (var card in _deck)
                _flipLimits[card] = FLIP_LIMIT_MAX;
        }
    }

    /// <summary>Returns a read-only view of the current deck.</summary>
    public IReadOnlyList<CardBase> GetDeck() => _deck.AsReadOnly();

    /// <summary>
    /// Called when the player clicks a card.
    /// Validates the click and manages flip logic internally.
    /// </summary>
    /// <returns>True if the flip was accepted; false if ignored.</returns>
    public bool OnCardSelected(CardBase card, int elapsedSeconds)
    {
        if (_boardLocked)   return false;
        if (card.IsMatched) return false;
        if (card.IsFaceUp)  return false;
        if (card == _firstFlipped || card == _secondFlipped) return false;

        // FlipLimit check — reject if this card is locked out
        if (_modifiers.HasFlag(GameModifier.FlipLimit))
        {
            if (_flipLimits.TryGetValue(card, out int remaining) && remaining <= 0)
                return false;
        }

        card.FlipUp();
        card.Reveal();

        // Decrement flip counter
        if (_modifiers.HasFlag(GameModifier.FlipLimit) && _flipLimits.ContainsKey(card))
            _flipLimits[card]--;

        // --- First card ---
        if (_firstFlipped == null)
        {
            _firstFlipped = card;
            return true;
        }

        // --- Second card ---
        if (_secondFlipped == null)
        {
            _secondFlipped = card;

            // TripleMatch: wait for a third card before evaluating
            if (_modifiers.HasFlag(GameModifier.TripleMatch))
                return true;

            EvaluatePair(elapsedSeconds);
            return true;
        }

        // --- Third card (TripleMatch only) ---
        _thirdFlipped = card;
        _moveCount++;
        _boardLocked = true;
        EvaluateTriple(elapsedSeconds);
        return true;
    }

    // ── Evaluate a 2-card flip ─────────────────────────────────────────────
    private void EvaluatePair(int elapsedSeconds)
    {
        _moveCount++;
        _boardLocked = true;

        CardBase first  = _firstFlipped!;
        CardBase second = _secondFlipped!;

        if (first.CardId == second.CardId)
            RegisterMatch(elapsedSeconds, first, second);
        else
            RegisterMismatch(first, second);
    }

    // ── Evaluate a 3-card flip ────────────────────────────────────────────
    private void EvaluateTriple(int elapsedSeconds)
    {
        // _firstFlipped, _secondFlipped, _thirdFlipped are all set before this is called
        CardBase first  = _firstFlipped!;
        CardBase second = _secondFlipped!;
        CardBase third  = _thirdFlipped!;

        bool match = first.CardId == second.CardId && second.CardId == third.CardId;

        if (match)
            RegisterMatch(elapsedSeconds, first, second, third);
        else
            RegisterMismatch(first, second, third);
    }

    // ── Register a successful match ───────────────────────────────────────
    private void RegisterMatch(int elapsedSeconds, params CardBase[] cards)
    {
        foreach (var c in cards) c.MarkAsMatched();
        _matchCount++;
        _score.RegisterMatch(elapsedSeconds, cards[0].PointValue);

        var args = new CardPairEventArgs(cards[0], cards[1], cards.Length > 2 ? cards[2] : null);
        MatchFound?.Invoke(this, args);

        ResetFlipGroup();
        _boardLocked = false;

        if (IsComplete) GameWon?.Invoke(this, EventArgs.Empty);
    }

    // ── Register a mismatch ───────────────────────────────────────────────
    private void RegisterMismatch(params CardBase[] cards)
    {
        _score.RegisterMismatch();

        if (_modifiers.HasFlag(GameModifier.HardcoreMode))
        {
            // Hardcore: game over immediately
            GameOverHardcore?.Invoke(this, EventArgs.Empty);
            return;
        }

        MismatchFound?.Invoke(this, new CardPairEventArgs(cards[0], cards[1],
            cards.Length > 2 ? cards[2] : null));
        // Board stays locked; caller calls FlipDownMismatch() after delay
    }

    /// <summary>
    /// Flips the mismatched group back down and unlocks the board.
    /// Call this after the UI's mismatch delay.
    /// </summary>
    public void FlipDownMismatch()
    {
        _firstFlipped?.FlipDown();
        _secondFlipped?.FlipDown();
        _thirdFlipped?.FlipDown();
        ResetFlipGroup();
        _boardLocked = false;
    }

    // ── CardDrift: randomise positions ────────────────────────────────────
    /// <summary>
    /// Shuffles the deck order (CardDrift modifier).
    /// Fires <see cref="CardsDrifted"/> with the new order so the UI can reposition cards.
    /// Only unmatched, face-down cards are shuffled.
    /// </summary>
    public void TriggerCardDrift()
    {
        // Separate matched (stay put) from unmatched (shuffle positions)
        var unmatchedIndices = _deck
            .Select((c, i) => (c, i))
            .Where(x => !x.c.IsMatched)
            .Select(x => x.i)
            .ToList();

        var shuffledIndices = ShuffleHelper.Shuffle(unmatchedIndices);

        // Rebuild deck with matched cards fixed, unmatched reshuffled
        var newDeck = _deck.ToList();
        for (int i = 0; i < unmatchedIndices.Count; i++)
            newDeck[unmatchedIndices[i]] = _deck[shuffledIndices[i]];

        _deck = newDeck;
        CardsDrifted?.Invoke(this, new DriftEventArgs(_deck.AsReadOnly()));
    }

    // ── Private Helpers ───────────────────────────────────────────────────
    private void ResetFlipGroup()
    {
        _firstFlipped  = null;
        _secondFlipped = null;
        _thirdFlipped  = null;
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

    /// <summary>Creates a fresh copy of a card with the same ID (for pair/triple building).</summary>
    private static CardBase CloneCard(CardBase source) => source switch
    {
        MonkeyCard  m => new MonkeyCard(m.CardId, m.Label, m.ImagePath, m.CharacterName, m.Personality),
        SpecialCard s => new SpecialCard(s.CardId, s.Label, s.ImagePath, s.SpecialAbility),
        _             => throw new InvalidOperationException("Unknown card type")
    };
}

// ── Event Args ────────────────────────────────────────────────────────────────

/// <summary>Event data carrying the cards involved in a match/mismatch (2 or 3).</summary>
public class CardPairEventArgs : EventArgs
{
    public CardBase  First  { get; }
    public CardBase  Second { get; }
    public CardBase? Third  { get; }   // non-null only in TripleMatch mode

    public CardPairEventArgs(CardBase first, CardBase second, CardBase? third = null)
    {
        First  = first;
        Second = second;
        Third  = third;
    }
}

/// <summary>Event data for CardDrift: carries the new deck order.</summary>
public class DriftEventArgs : EventArgs
{
    public IReadOnlyList<CardBase> NewOrder { get; }
    public DriftEventArgs(IReadOnlyList<CardBase> order) => NewOrder = order;
}