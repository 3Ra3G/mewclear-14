// Misfits Change - Shows a red exclamation mark above mobs that have recently entered combat,
// and above players/NPCs while combat mode (Num1) is active.
using Content.Shared._Misfits.Sound;
using Content.Shared.CombatMode;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Misfits.StatusIcon;

/// <summary>
/// Shows a red exclamation mark status icon in two cases:
/// 1. Above any mob with <see cref="AggroSoundComponent"/> while its aggro cooldown is active.
/// 2. Above any entity with <see cref="CombatModeComponent"/> while combat mode is toggled on.
/// Visible to all nearby players — no HUD equipment required.
/// </summary>
public sealed class ShowAggroIconSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AggroSoundComponent, GetStatusIconsEvent>(OnGetAggroStatusIcons);
        SubscribeLocalEvent<CombatModeComponent, GetStatusIconsEvent>(OnGetCombatModeStatusIcons);
    }

    private void OnGetAggroStatusIcons(EntityUid uid, AggroSoundComponent comp, ref GetStatusIconsEvent ev)
    {
        if (comp.CooldownRemaining <= 0f)
            return;

        if (_prototype.TryIndex<FactionIconPrototype>("N14AggroIcon", out var icon))
            ev.StatusIcons.Add(icon);
    }

    private void OnGetCombatModeStatusIcons(EntityUid uid, CombatModeComponent comp, ref GetStatusIconsEvent ev)
    {
        if (!comp.IsInCombatMode)
            return;

        if (_prototype.TryIndex<FactionIconPrototype>("N14AggroIcon", out var icon))
            ev.StatusIcons.Add(icon);
    }
}
