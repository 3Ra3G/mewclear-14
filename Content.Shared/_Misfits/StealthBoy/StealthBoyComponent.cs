// #Misfits Add - Stealth Boy item component. When activated, grants the holder
// temporary active invisibility for Duration seconds. Single-use, consumed on activation.
using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.StealthBoy;

/// <summary>
/// Placed on the Stealth Boy item. Activating it (Z-key / Use In Hand) applies
/// temporary invisibility to the activating player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StealthBoyComponent : Component
{
    /// <summary>
    /// How long the invisibility lasts once activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Fade-in time — how long it takes to reach minimum opacity.
    /// </summary>
    [DataField]
    public TimeSpan FadeInTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Fade-out time — how long it takes to return to full opacity after deactivation.
    /// </summary>
    [DataField]
    public TimeSpan FadeOutTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Minimum opacity while cloaked (0 = fully invisible, 1 = fully visible).
    /// </summary>
    [DataField]
    public float MinOpacity = 0.15f;
}
