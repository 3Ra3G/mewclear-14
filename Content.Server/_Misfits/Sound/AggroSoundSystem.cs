// Misfits Change - System to play aggro/alert sounds on combat entry, separate from idle ambient sounds
using Content.Shared._Misfits.Sound;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server._Misfits.Sound;

/// <summary>
/// Plays an aggro/alert sound the first time an entity with
/// <see cref="AggroSoundComponent"/> attacks (melee or ranged), with a cooldown
/// to prevent spam. Keeps combat vocalizations separate from idle ambient sounds.
/// </summary>
public sealed class AggroSoundSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AggroSoundComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<AggroSoundComponent, GunShotEvent>(OnGunShot);
    }

    private void OnMeleeAttack(Entity<AggroSoundComponent> entity, ref MeleeAttackEvent args)
    {
        TryPlayAggro(entity);
    }

    private void OnGunShot(Entity<AggroSoundComponent> entity, ref GunShotEvent args)
    {
        // GunShotEvent fires on the gun entity. For mobs that ARE their own gun
        // (Gun component directly on the mob), this fires on the mob itself.
        TryPlayAggro(entity);
    }

    private void TryPlayAggro(Entity<AggroSoundComponent> entity)
    {
        if (entity.Comp.CooldownRemaining > 0f)
            return;

        _audio.PlayPvs(entity.Comp.Sound, entity.Owner);
        entity.Comp.CooldownRemaining = entity.Comp.CooldownDuration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AggroSoundComponent>();
        while (query.MoveNext(out _, out var aggro))
        {
            if (aggro.CooldownRemaining > 0f)
                aggro.CooldownRemaining -= frameTime;
        }
    }
}
