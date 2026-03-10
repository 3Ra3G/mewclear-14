// #Misfits Add - Server system managing persistent player SPECIAL stats, kill/death/round counters,
// and character history log. Data persists across rounds via a flat player_data.json file.
using System.IO;
using System.Linq;
using System.Text.Json;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._Misfits.PlayerData;
using Content.Shared._Misfits.PlayerData.Components;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;

namespace Content.Server._Misfits.PlayerData;

/// <summary>
/// Loads/saves player data (SPECIAL, statistics, history) from <c>player_data.json</c>.
/// Tracks mob kills via <see cref="MobStateChangedEvent"/> origin attribution.
/// Tracks player deaths via the same event on entities that own the component.
/// </summary>
public sealed class PersistentPlayerDataSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    // #Misfits Add - Sawmill for data system logging
    private ISawmill _log = default!;

    private const string DataFileName = "player_data.json";
    private readonly Dictionary<string, CharacterPlayerData> _playerData = new();
    private string _saveFilePath = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("persistent_player_data");

        // Component-scoped events (fire only when entity has the component)
        SubscribeLocalEvent<PersistentPlayerDataComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PersistentPlayerDataComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PersistentPlayerDataComponent, MobStateChangedEvent>(OnPlayerMobStateChanged);

        // Global event — track kills when any mob dies with a known origin
        SubscribeLocalEvent<MobStateChangedEvent>(OnAnyMobStateChanged);

        // Ensure the component is added when a player spawns (required for loading to trigger)
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        // #Misfits Add - Handle SPECIAL allocation confirmation from client
        SubscribeNetworkEvent<ConfirmSpecialAllocationEvent>(OnConfirmSpecialAllocation);

        // Determine save path from user-data directory
        var userDataPath = _resourceManager.UserData.RootDir ?? ".";
        _saveFilePath = Path.Combine(userDataPath, DataFileName);

        LoadAllData();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a text entry to the character's history log (capped at 50 entries) and saves.
    /// </summary>
    public void AddHistoryEntry(EntityUid playerUid, string entry)
    {
        if (!TryComp<PersistentPlayerDataComponent>(playerUid, out var comp))
            return;

        AppendHistory(comp, entry);
        Dirty(playerUid, comp);
        SavePlayer(comp);
    }

    // ── Spawn / Attach ─────────────────────────────────────────────────────────

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Ensure the persistent data component exists on the player entity
        var comp = EnsureComp<PersistentPlayerDataComponent>(args.Mob);

        // Load immediately if the player is already attached (they will be for normal spawns)
        if (args.Player.AttachedEntity == args.Mob)
            LoadPlayer(args.Mob, comp, args.Player);
    }

    private void OnPlayerAttached(Entity<PersistentPlayerDataComponent> ent, ref PlayerAttachedEvent args)
    {
        // Handles reconnects and late-attachment; LoadPlayer is idempotent
        LoadPlayer(ent, ent.Comp, args.Player);
    }

    // ── Shutdown / Save ────────────────────────────────────────────────────────

    private void OnShutdown(Entity<PersistentPlayerDataComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Loaded)
            SavePlayer(ent.Comp);
    }

    // ── Death / Kill tracking ──────────────────────────────────────────────────

    /// <summary>
    /// Fires on the player entity's own state change → track player deaths.
    /// </summary>
    private void OnPlayerMobStateChanged(Entity<PersistentPlayerDataComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (ent.Comp.DiedThisRound)
            return; // already counted this round

        ent.Comp.DiedThisRound = true;
        ent.Comp.Deaths++;

        AppendHistory(ent.Comp, $"Died in the Wasteland (round {ent.Comp.RoundsPlayed}).");
        Dirty(ent, ent.Comp);
        SavePlayer(ent.Comp);
    }

    /// <summary>
    /// Fires on ALL mob state changes. If a non-player mob dies and Origin has player data,
    /// credit a mob kill to that player.
    /// </summary>
    private void OnAnyMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (args.Origin == null)
            return;

        // Dying entity must NOT be a player (must not have PersistentPlayerDataComponent)
        if (HasComp<PersistentPlayerDataComponent>(args.Target))
            return;

        // Killer must be a player
        if (!TryComp<PersistentPlayerDataComponent>(args.Origin.Value, out var killerComp))
            return;

        killerComp.MobKills++;
        Dirty(args.Origin.Value, killerComp);
        SavePlayer(killerComp);
    }

    // ── SPECIAL confirmation ───────────────────────────────────────────────────

    // #Misfits Add - Validates and applies the player's S.P.E.C.I.A.L. allocation then locks it.
    private void OnConfirmSpecialAllocation(ConfirmSpecialAllocationEvent msg, EntitySessionEventArgs args)
    {
        var entity = args.SenderSession.AttachedEntity;
        if (entity == null)
            return;

        if (!TryComp<PersistentPlayerDataComponent>(entity.Value, out var comp))
            return;

        if (comp.StatsConfirmed)
            return; // already locked — ignore replay

        // Validate: each stat 1–10 and total budget ≤ 17 (7 minimum + 10 allocated)
        var vals = new[] { msg.Strength, msg.Perception, msg.Endurance, msg.Charisma, msg.Intelligence, msg.Agility, msg.Luck };
        if (vals.Any(v => v < 1 || v > 10) || vals.Sum() > 17)
            return;

        comp.Strength     = msg.Strength;
        comp.Perception   = msg.Perception;
        comp.Endurance    = msg.Endurance;
        comp.Charisma     = msg.Charisma;
        comp.Intelligence = msg.Intelligence;
        comp.Agility      = msg.Agility;
        comp.Luck         = msg.Luck;
        comp.StatsConfirmed = true;

        AppendHistory(comp, $"Allocated S.P.E.C.I.A.L.: S{msg.Strength} P{msg.Perception} E{msg.Endurance} C{msg.Charisma} I{msg.Intelligence} A{msg.Agility} L{msg.Luck}.");
        Dirty(entity.Value, comp);
        SavePlayer(comp);
    }

    // ── Data load/save helpers ─────────────────────────────────────────────────

    private void LoadPlayer(EntityUid uid, PersistentPlayerDataComponent comp, ICommonSession session)
    {
        if (comp.Loaded)
            return;

        // Mind may not be ready at startup — only proceed when it is
        if (!_mind.TryGetMind(uid, out _, out var mind))
            return;

        var characterName = mind.CharacterName;
        if (string.IsNullOrEmpty(characterName))
            return;

        comp.UserId = session.UserId.ToString();
        comp.CharacterName = characterName;

        var key = BuildKey(comp.UserId, characterName);
        if (_playerData.TryGetValue(key, out var saved))
        {
            comp.Strength = saved.Strength;
            comp.Perception = saved.Perception;
            comp.Endurance = saved.Endurance;
            comp.Charisma = saved.Charisma;
            comp.Agility = saved.Agility;
            comp.Intelligence = saved.Intelligence;
            comp.Luck = saved.Luck;

            comp.MobKills = saved.MobKills;
            comp.Deaths = saved.Deaths;
            comp.RoundsPlayed = saved.RoundsPlayed;
            comp.HistoryLog = new List<string>(saved.HistoryLog);
            comp.StatsConfirmed = saved.StatsConfirmed; // #Misfits Fix - only lock if the player explicitly confirmed previously
        }
        else
        {
            // First-time character — welcome entry
            AppendHistory(comp, "Arrived in the Wasteland for the first time.");
        }

        // Increment round counter exactly once per spawn
        if (!comp.RoundCountedThisRound)
        {
            comp.RoundsPlayed++;
            comp.RoundCountedThisRound = true;

            if (comp.RoundsPlayed > 1)
                AppendHistory(comp, $"Entered the Wasteland for round {comp.RoundsPlayed}.");
        }

        comp.Loaded = true;
        Dirty(uid, comp);
        SavePlayer(comp);
    }

    private void SavePlayer(PersistentPlayerDataComponent comp)
    {
        if (string.IsNullOrEmpty(comp.UserId) || string.IsNullOrEmpty(comp.CharacterName))
            return;

        var key = BuildKey(comp.UserId, comp.CharacterName);
        _playerData[key] = new CharacterPlayerData
        {
            UserId = comp.UserId,
            CharacterName = comp.CharacterName,

            Strength = comp.Strength,
            Perception = comp.Perception,
            Endurance = comp.Endurance,
            Charisma = comp.Charisma,
            Agility = comp.Agility,
            Intelligence = comp.Intelligence,
            Luck = comp.Luck,

            MobKills = comp.MobKills,
            Deaths = comp.Deaths,
            RoundsPlayed = comp.RoundsPlayed,

            StatsConfirmed = comp.StatsConfirmed,
            HistoryLog = new List<string>(comp.HistoryLog),
        };

        PersistAllData();
    }

    private static void AppendHistory(PersistentPlayerDataComponent comp, string entry)
    {
        comp.HistoryLog.Add(entry);
        // Cap to 50 entries — remove oldest when exceeded
        if (comp.HistoryLog.Count > 50)
            comp.HistoryLog.RemoveAt(0);
    }

    private static string BuildKey(string userId, string characterName) => $"{userId}:{characterName}";

    private void LoadAllData()
    {
        try
        {
            if (!File.Exists(_saveFilePath))
                return;

            var json = File.ReadAllText(_saveFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, CharacterPlayerData>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
                _playerData[kvp.Key] = kvp.Value;

            _log.Debug($"Loaded {_playerData.Count} character records from {DataFileName}.");
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load {DataFileName}: {ex}");
        }
    }

    private void PersistAllData()
    {
        try
        {
            var json = JsonSerializer.Serialize(_playerData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to save {DataFileName}: {ex}");
        }
    }
}

/// <summary>
/// JSON-serializable data model for one character's persistent record.
/// New fields default gracefully when reading older save files.
/// </summary>
public sealed class CharacterPlayerData
{
    public string UserId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;

    // SPECIAL defaults to 1; old saves have explicit stored values so this only affects missing fields
    public int Strength { get; set; } = 1;
    public int Perception { get; set; } = 1;
    public int Endurance { get; set; } = 1;
    public int Charisma { get; set; } = 1;
    public int Agility { get; set; } = 1;
    public int Intelligence { get; set; } = 1;
    public int Luck { get; set; } = 1;

    public int MobKills { get; set; }
    public int Deaths { get; set; }
    public int RoundsPlayed { get; set; }

    public bool StatsConfirmed { get; set; } = false;
    public List<string> HistoryLog { get; set; } = new();
}
