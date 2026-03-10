// #Misfits Change Add: Handles deflection of thrown Spear-tagged weapons (spears, javelins, polearms)
// by entities wearing or holding items with SpearBlockComponent (shields, power armor).
//
// Mirrors ReflectSystem's ReflectUserComponent propagation pattern.
// When a hit triggers, the spear is prevented from embedding (temporary ThrownItemImmuneComponent)
// and a narrative popup is shown: "[Thrower]'s [spear] was deflected by [target] and fell to the ground!"
using Content.Shared._Misfits.Throwing.Components;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Misfits.Throwing;

public sealed class SpearBlockSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Propagate SpearBlockUserComponent to the equipee when a SpearBlock item is worn or held.
        SubscribeLocalEvent<SpearBlockComponent, GotEquippedEvent>(OnSpearBlockEquipped);
        SubscribeLocalEvent<SpearBlockComponent, GotUnequippedEvent>(OnSpearBlockUnequipped);
        SubscribeLocalEvent<SpearBlockComponent, GotEquippedHandEvent>(OnSpearBlockHandEquipped);
        SubscribeLocalEvent<SpearBlockComponent, GotUnequippedHandEvent>(OnSpearBlockHandUnequipped);

        // Handle incoming thrown polearms on entities that carry a SpearBlock item.
        SubscribeLocalEvent<SpearBlockUserComponent, ThrowHitByEvent>(OnSpearHitUser);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Clean up temporary ThrownItemImmuneComponent added to prevent spear embedding.
        // The immunity must persist until ThrowDoHitEvent fires (synchronously after ThrowHitByEvent)
        // and is safe to remove on the very next tick.
        var query = EntityQueryEnumerator<SpearBlockCleanupComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemComp<ThrownItemImmuneComponent>(uid);
            RemComp<SpearBlockCleanupComponent>(uid);
        }
    }

    // --- Equipment event handlers (mirror of ReflectSystem pattern) ---

    private void OnSpearBlockEquipped(EntityUid uid, SpearBlockComponent comp, GotEquippedEvent args)
    {
        if (_gameTiming.ApplyingState)
            return;
        EnsureComp<SpearBlockUserComponent>(args.Equipee);
    }

    private void OnSpearBlockUnequipped(EntityUid uid, SpearBlockComponent comp, GotUnequippedEvent args)
    {
        RefreshSpearBlockUser(args.Equipee, uid);
    }

    private void OnSpearBlockHandEquipped(EntityUid uid, SpearBlockComponent comp, GotEquippedHandEvent args)
    {
        if (_gameTiming.ApplyingState)
            return;
        EnsureComp<SpearBlockUserComponent>(args.User);
    }

    private void OnSpearBlockHandUnequipped(EntityUid uid, SpearBlockComponent comp, GotUnequippedHandEvent args)
    {
        RefreshSpearBlockUser(args.User, uid);
    }

    /// <summary>
    /// Removes SpearBlockUserComponent from <paramref name="user"/> unless another equipped item
    /// (other than <paramref name="excluding"/>) still provides SpearBlockComponent.
    /// </summary>
    private void RefreshSpearBlockUser(EntityUid user, EntityUid excluding)
    {
        if (!HasComp<SpearBlockUserComponent>(user))
            return;

        foreach (var ent in _inventorySystem.GetHandOrInventoryEntities(user, SlotFlags.All & ~SlotFlags.POCKET))
        {
            if (ent != excluding && HasComp<SpearBlockComponent>(ent))
                return; // Another item still provides the block.
        }

        RemCompDeferred<SpearBlockUserComponent>(user);
    }

    // --- Deflection handler ---

    private void OnSpearHitUser(EntityUid uid, SpearBlockUserComponent comp, ThrowHitByEvent args)
    {
        // Only block Spear-tagged thrown items (spears, javelins, polearms).
        if (!_tagSystem.HasTag(args.Thrown, "Spear"))
            return;

        // Add ThrownItemImmuneComponent BEFORE ThrowDoHitEvent fires (which follows this event
        // synchronously in ThrowCollideInteraction). OnEmbedThrowDoHit checks this flag to skip embedding.
        // SpearBlockCleanupComponent signals Update() to remove it next tick.
        if (_netManager.IsServer && !HasComp<ThrownItemImmuneComponent>(uid))
        {
            AddComp<ThrownItemImmuneComponent>(uid);
            EnsureComp<SpearBlockCleanupComponent>(uid);
        }

        if (!_netManager.IsServer)
            return;

        // Show narrative popup visible to all nearby players.
        var spearName = Name(args.Thrown);
        var targetName = Identity.Name(uid, EntityManager);
        var throwerName = args.User is { } throwerId
            ? Identity.Name(throwerId, EntityManager)
            : Loc.GetString("spear-block-unknown-thrower");

        _popup.PopupEntity(
            Loc.GetString("spear-block-deflected",
                ("thrower", throwerName), ("spear", spearName), ("target", targetName)),
            uid,
            PopupType.Medium);
    }
}
