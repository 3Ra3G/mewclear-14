using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Misfits.CCVar;
using Content.Shared._Misfits.NPC;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Misfits.NPC;

/// <summary>
/// Keeps NPCs with <see cref="ProximityNPCComponent"/> asleep until a player enters
/// their wake radius, then re-sleeps them when all players leave.
///
/// Why: Wendover is ~8000×4190 tiles. Running HTN planning on every creature at all
/// times saturates server CPU long before the player pop limit matters. By sleeping
/// distant NPCs we get near-zero per-tick cost for the majority of the map's fauna.
///
/// How it differs from RMC-14: RMC wakes xenonids when the dropship lands on-planet.
/// We instead perform a periodic spatial query against connected player positions,
/// which works for an always-on-grid (no vessel/space) game mode.
/// </summary>
public sealed class ProximityNPCSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private float _accumulator;
    private float _checkInterval;

    // Reused across calls to avoid allocating a new HashSet per NPC per scan.
    private readonly HashSet<Entity<ActorComponent>> _playerBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, PerformanceCVars.ProximityNPCCheckInterval, v => _checkInterval = v, true);

        // Subscribe AFTER HTNSystem so our sleep call overrides HTN's default WakeNPC on map init.
        SubscribeLocalEvent<ProximityNPCComponent, MapInitEvent>(OnMapInit,
            after: [typeof(HTNSystem)]);
    }

    private void OnMapInit(Entity<ProximityNPCComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.StartAsleep)
            _npc.SleepNPC(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < _checkInterval)
            return;
        _accumulator -= _checkInterval;

        var query = EntityQueryEnumerator<ProximityNPCComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var prox, out var xform))
        {
            if (xform.MapID == MapId.Nullspace)
                continue;

            var mapPos = _transform.GetMapCoordinates(uid, xform);
            var awake = _npc.IsAwake(uid);

            if (!awake)
            {
                // Sleeping — wake if any player has entered the wake radius.
                if (HasPlayerWithin(mapPos, prox.WakeRange))
                    _npc.WakeNPC(uid);
            }
            else
            {
                // Awake — sleep if all players have left the sleep radius.
                // The sleep radius being larger than the wake radius prevents thrashing.
                if (!HasPlayerWithin(mapPos, prox.SleepRange))
                    _npc.SleepNPC(uid);
            }
        }
    }

    /// <summary>
    /// Returns true if at least one player-controlled entity is within <paramref name="range"/>
    /// tiles of <paramref name="pos"/> on the same map.
    /// </summary>
    private bool HasPlayerWithin(MapCoordinates pos, float range)
    {
        // ActorComponent is the marker for a session-controlled entity.
        // Use the overload that populates a reusable HashSet to avoid per-call heap allocation.
        _playerBuffer.Clear();
        _lookup.GetEntitiesInRange(pos, range, _playerBuffer);
        return _playerBuffer.Count > 0;
    }
}
