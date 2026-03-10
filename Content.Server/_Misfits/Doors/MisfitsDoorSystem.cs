// #Misfits Change/Add: Server-side handler for door access denial feedback.
// Shows a popup message to the player who attempted to open a locked door without the required access.

using Content.Shared._Misfits.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Popups;

namespace Content.Server._Misfits.Doors;

/// <summary>
/// Handles player feedback when a door denies them access.
/// Listens for <see cref="DoorDeniedEvent"/> and shows a small popup message to the denied player.
/// </summary>
public sealed class MisfitsDoorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to the door denied event raised by SharedDoorSystem.Deny().
        SubscribeLocalEvent<DoorComponent, DoorDeniedEvent>(OnDoorDenied);
    }

    /// <summary>
    /// Sends a popup to the player informing them that the door will not open for them.
    /// Only fires server-side; client-side the visual deny animation provides feedback.
    /// </summary>
    private void OnDoorDenied(EntityUid uid, DoorComponent comp, DoorDeniedEvent args)
    {
        // No user to notify (e.g. automated trigger) — nothing to do.
        if (args.User == null)
            return;

        // Show the message near the door so the player sees it in context.
        _popup.PopupEntity(
            Loc.GetString("door-access-denied-popup"),
            uid,
            args.User.Value,
            PopupType.SmallCaution);
    }
}
