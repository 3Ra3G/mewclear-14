using Robust.Client.Timing;
using Robust.Shared.Timing;

namespace Content.Client._Misfits.Movement;

/// <summary>
/// Client-side lag compensation system. Reads the engine's last-confirmed real tick via
/// <see cref="IClientGameTiming.LastRealTick"/> and exposes it for client prediction code
/// (gun fire, action use) to stamp onto outgoing events.
///
/// The stamped tick is piggybacked on <c>RequestShootEvent</c> and <c>RequestPerformActionEvent</c>
/// which are already sent as predictive events — no separate periodic message is needed,
/// avoiding the "Got late MsgEntity" warning caused by tick-stamped entity events on a timer.
/// </summary>
public sealed class MisfitsLagCompensationSystem : EntitySystem
{
    [Dependency] private readonly IClientGameTiming _clientTiming = default!;

    /// <summary>
    /// Returns the client's last confirmed engine tick. Stamp this onto any outgoing
    /// event that the server will use for lag-compensated range validation.
    /// </summary>
    public GameTick GetLastRealTick() => _clientTiming.LastRealTick;
}
