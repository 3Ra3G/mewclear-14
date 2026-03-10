// Misfits Change - Marker component enabling vision of mob aggro status icons
using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.StatusIcon;

/// <summary>
/// Allows the player to see aggro/combat-entry exclamation icons above hostile mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowAggroIconsComponent : Component { }
