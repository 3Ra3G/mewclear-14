namespace Content.Shared._Misfits.NPC;

/// <summary>
/// Marks an NPC as using proximity-based sleep/wake.
/// The NPC starts asleep on map initialisation and wakes only when a player-controlled
/// entity enters <see cref="WakeRange"/> tiles. It re-sleeps when all players leave
/// <see cref="SleepRange"/> tiles.
///
/// Designed for large open maps (e.g. Wendover at 8000×4190) where running full HTN
/// AI on every creature continuously is too expensive.
/// </summary>
[RegisterComponent]
public sealed partial class ProximityNPCComponent : Component
{
    /// <summary>
    /// Distance (tiles) within which a player wakes this NPC.
    /// </summary>
    [DataField]
    public float WakeRange = 40f;

    /// <summary>
    /// Distance (tiles) at which the NPC sleeps if no players remain nearby.
    /// Must be greater than <see cref="WakeRange"/> to create hysteresis and prevent
    /// rapid wake/sleep thrashing at the boundary edge.
    /// </summary>
    [DataField]
    public float SleepRange = 60f;

    /// <summary>
    /// If true, overrides the default HTN behaviour of waking on map init and instead
    /// keeps this NPC asleep until a player enters its wake range.
    /// </summary>
    [DataField]
    public bool StartAsleep = true;
}
