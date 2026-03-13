// #Misfits Add - Applied to a player while their Stealth Boy is active.
// Tracks the cloak end time and current opacity phase for smooth fading.
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Misfits.StealthBoy;

/// <summary>
/// Applied to the player entity (not the item) while actively cloaked.
/// Client reads Opacity to drive sprite alpha.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class StealthBoyActiveComponent : Component
{
    /// <summary>
    /// When the cloak expires (server time).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan EndTime;

    /// <summary>
    /// When the cloak was activated (for fade-in interpolation).
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan StartTime;

    /// <summary>
    /// Current rendered opacity — interpolated by the client-side visualizer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Opacity = 1f;

    /// <summary>
    /// Minimum opacity while fully cloaked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinOpacity = 0.15f;

    /// <summary>
    /// Fade-in duration copied from StealthBoyComponent at activation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeInTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Whether the cloak is in fade-out phase (device duration expired but still fading).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FadingOut;

    /// <summary>
    /// When the fade-out started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan FadeOutStart;

    /// <summary>
    /// Fade-out duration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeOutTime = TimeSpan.FromSeconds(2);
}
