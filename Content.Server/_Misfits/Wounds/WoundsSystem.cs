// #Misfits Add - WoundsSystem (server): ticks bleed damage every second.
// Removes expired / treated wounds. Applies bloodloss damage via DamageableSystem.
using Content.Shared._Misfits.Wounds;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.Wounds;

public sealed class WoundsSystem : SharedWoundsSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _protoServer = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<WoundedComponent, WoundableComponent>();

        while (query.MoveNext(out var uid, out var wounded, out var woundable))
        {
            if (now < wounded.UpdateAt)
                continue;

            wounded.UpdateAt = now + wounded.UpdateCooldown;

            // Tick each wound
            for (var i = wounded.Wounds.Count - 1; i >= 0; i--)
            {
                var wound = wounded.Wounds[i];

                // Remove expired wounds
                if (wound.StopBleedAt != null && wound.StopBleedAt <= now)
                {
                    wounded.Wounds.RemoveAt(i);
                    continue;
                }

                if (wound.Treated)
                    continue;

                // Apply bloodloss damage (maps to Bloodloss damage type for Shitmed compat)
                var bleedDamage = new DamageSpecifier();
                bleedDamage.DamageDict.Add("Bloodloss", wound.Bloodloss);
                _dmg.TryChangeDamage(uid, bleedDamage, ignoreResistances: true);
            }

            Dirty(uid, wounded);

            if (wounded.Wounds.Count == 0)
                RemCompDeferred<WoundedComponent>(uid);
        }
    }

    protected override void PlaySoundClient(SoundSpecifier sound, EntityUid source)
    {
        _audio.PlayPvs(sound, source);
    }
}
