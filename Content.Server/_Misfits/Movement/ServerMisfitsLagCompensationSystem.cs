using Content.Shared._Misfits.CCVar;
using Content.Shared.Actions;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.Movement;

/// <summary>
/// Server-side lag compensation system. Piggybacks on the <c>LastRealTick</c> field
/// already stamped on <see cref="RequestShootEvent"/> and <see cref="RequestPerformActionEvent"/>
/// by the client, storing it per-session. Range-validation calls can then apply a small
/// tolerance margin when a player's action was sent from a behind-tick snapshot.
///
/// No separate heartbeat message is used — the tick is always read from the most recent
/// latency-sensitive event the player raised. This avoids the "Got late MsgEntity" spam
/// that arises when sending tick-stamped entity events on a periodic timer.
/// </summary>
public sealed class ServerMisfitsLagCompensationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Per-session last-real-tick extracted from the client's stamped events.
    private readonly Dictionary<NetUserId, GameTick> _lastRealTicks = new();

    /// <summary>Additional range buffer (tiles) applied when client is behind the server.</summary>
    public float MarginTiles { get; private set; }

    /// <summary>Maximum lag window (ms) the server compensates for.</summary>
    public int MaxCompensationMs { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, PerformanceCVars.LagCompensationMarginTiles, v => MarginTiles = v, true);
        Subs.CVar(_config, PerformanceCVars.LagCompensationMs, v => MaxCompensationMs = v, true);

        // Read LastRealTick directly from the events the client already sends for game actions.
        // This avoids needing a separate periodic heartbeat message (which caused "Got late MsgEntity" spam).
        SubscribeNetworkEvent<RequestShootEvent>(OnReceiveShootEvent);
        SubscribeNetworkEvent<RequestPerformActionEvent>(OnReceiveActionEvent);

        // Clean up stored ticks when a player disconnects to prevent a slow memory leak.
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        // Only clean up on true disconnect, not just detachment from a body.
        if (ev.Player.Status != Robust.Shared.Enums.SessionStatus.Disconnected)
            return;

        _lastRealTicks.Remove(ev.Player.UserId);
    }

    private void OnReceiveShootEvent(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        if (msg.LastRealTick is not { } tick)
            return;

        // Store tick - 1: the last fully-received world state the client acted on.
        _lastRealTicks[args.SenderSession.UserId] = tick - 1;
    }

    private void OnReceiveActionEvent(RequestPerformActionEvent msg, EntitySessionEventArgs args)
    {
        if (msg.LastRealTick is not { } tick)
            return;

        _lastRealTicks[args.SenderSession.UserId] = tick - 1;
    }

    /// <summary>
    /// Returns the last confirmed engine tick for <paramref name="session"/>,
    /// or the current server tick if the client has not yet sent a heartbeat.
    /// </summary>
    public GameTick GetLastRealTick(ICommonSession session) =>
        _lastRealTicks.GetValueOrDefault(session.UserId, _timing.CurTick);

    /// <summary>
    /// Returns the last confirmed engine tick for the entity's controlling player session,
    /// or the current server tick if the entity has no player or has not sent a heartbeat.
    /// </summary>
    public GameTick GetLastRealTick(EntityUid ent)
    {
        if (!TryComp<ActorComponent>(ent, out var actor))
            return _timing.CurTick;

        return GetLastRealTick(actor.PlayerSession);
    }

    /// <summary>
    /// Checks whether <paramref name="target"/> is within <paramref name="range"/> tiles of
    /// <paramref name="origin"/>, optionally adding <see cref="MarginTiles"/> if the session's
    /// last confirmed tick is behind the server by at most <see cref="MaxCompensationMs"/> ms.
    ///
    /// Use this instead of bare <c>TransformSystem.InRange</c> on any server-side handler that
    /// validates player-originated ranged interactions.
    /// </summary>
    public bool IsWithinRange(EntityUid origin, EntityUid target, ICommonSession session, float range)
    {
        var originCoords = Transform(origin).Coordinates;
        var targetCoords = Transform(target).Coordinates;

        var effectiveRange = range;

        var storedTick = GetLastRealTick(session);
        var tickDelta = (int)(_timing.CurTick.Value - storedTick.Value);
        var msLag = tickDelta * (1000f / _timing.TickRate);

        // If the client was behind the server (within the compensation window), add margin
        // to forgive minor positional drift that occurred between ticks.
        if (msLag > 0 && msLag <= MaxCompensationMs)
            effectiveRange += MarginTiles;

        return _transform.InRange(originCoords, targetCoords, effectiveRange);
    }
}
