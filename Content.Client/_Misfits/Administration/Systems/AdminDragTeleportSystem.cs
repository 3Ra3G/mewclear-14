// #Misfits Change/Add: Client-side system that intercepts drag-drop ending on empty space for admins,
// teleporting the dragged entity to the cursor release position.
using Content.Client._Misfits.Administration.Events;
using Content.Client.Administration.Managers;
using Content.Shared._Misfits.Administration;
using Content.Shared.DragDrop;
using Robust.Client.GameObjects;

namespace Content.Client._Misfits.Administration.Systems;

/// <summary>
/// Allows admins to initiate a drag on ANY entity (via <see cref="CanDragEvent"/>) and
/// teleport it to wherever they release the cursor when no valid entity drop target is found.
/// <para>
/// Two responsibilities:
/// 1. Subscribe <see cref="CanDragEvent"/> on <see cref="SpriteComponent"/> (present on all
///    visible entities) so admins can start a drag on entities that normally don't support it.
/// 2. Subscribe <see cref="DragNoTargetEvent"/> to send <see cref="AdminSelfDragTeleportEvent"/>
///    to the server when the drag ends on empty space.
/// </para>
/// </summary>
public sealed class AdminDragTeleportSystem : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Allow admins to drag any entity that has a sprite (i.e. every visible entity).
        // CanDragEvent has no user context, so we use the local admin status as the gate.
        SubscribeLocalEvent<SpriteComponent, CanDragEvent>(OnCanDrag);

        // Intercept drag-drops that ended without a valid entity target.
        SubscribeLocalEvent<DragNoTargetEvent>(OnDragNoTarget);
    }

    /// <summary>
    /// Marks any sprite-bearing entity as draggable when the local player is an admin.
    /// This is purely client-side; no gameplay change occurs until the drag is released.
    /// </summary>
    private void OnCanDrag(EntityUid uid, SpriteComponent component, ref CanDragEvent args)
    {
        if (!_adminManager.IsAdmin())
            return;

        args.Handled = true;
    }

    private void OnDragNoTarget(DragNoTargetEvent ev)
    {
        // Require admin privileges on the client (server will re-validate before acting).
        if (!_adminManager.IsAdmin())
            return;

        // Send the teleport request to the server with both the entity and target coordinates.
        RaiseNetworkEvent(new AdminSelfDragTeleportEvent(
            GetNetEntity(ev.DraggedEntity),
            GetNetCoordinates(ev.TargetCoordinates)));

        // Mark as handled so DragDropSystem signals a successful drop instead of a cancel.
        ev.Handled = true;
    }
}
