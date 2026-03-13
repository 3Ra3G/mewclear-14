// #Misfits Add - WoundsEvents: DoAfter events and raised events for the wound system.
// Simplified from RMC-14 — no skills, no RMC-specific event types.
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Wounds;

[Serializable, NetSerializable]
public sealed partial class TreatWoundDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Raised on a WoundableComponent entity to allow vetoing bleed. Cancel to prevent it.
/// </summary>
[ByRefEvent]
public record struct CMBleedAttemptEvent(bool Cancelled);

/// <summary>
/// Raised on a WoundableComponent entity after a bleed damage event. Set Handled = true to skip default bleed logic.
/// </summary>
[ByRefEvent]
public record struct CMBleedEvent(DamageChangedEvent Damage, bool Handled = false);
