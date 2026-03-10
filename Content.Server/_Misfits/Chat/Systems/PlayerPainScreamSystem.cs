// #Misfits Change - Player-controlled mobs scream on barbed-wire style hazards, bleed start, and severe impacts.
using Content.Server.Chat.Systems;
using Content.Server.Damage.Components;
using Content.Server.Damage.Systems;
using Content.Server._Misfits.Chat.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.Chat.Systems;

/// <summary>
/// Triggers scream emotes for attached player mobs when they hit painful hazard milestones.
/// </summary>
public sealed class PlayerPainScreamSystem : EntitySystem
{
    private static readonly TimeSpan ScreamCooldown = TimeSpan.FromSeconds(2.5);
    private const float HeavyImpactDamageThreshold = 15f;

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextScreamTime = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, PlayerDetachedEvent>(OnActorShutdown);
        SubscribeLocalEvent<ActorComponent, DamageChangedEvent>(OnPlayerDamaged);
        SubscribeLocalEvent<ActorComponent, BleedAmountChangedEvent>(OnBleedAmountChanged);
        SubscribeLocalEvent<DamageUserOnTriggerComponent, BeforeDamageUserOnTriggerEvent>(OnBeforeDamageUserOnTrigger);
    }

    private void OnActorShutdown(Entity<ActorComponent> ent, ref PlayerDetachedEvent args)
    {
        _nextScreamTime.Remove(ent);
    }

    private void OnPlayerDamaged(Entity<ActorComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is null)
            return;

        if (args.DamageDelta.GetTotal().Float() < HeavyImpactDamageThreshold)
            return;

        TryScream(ent);
    }

    private void OnBleedAmountChanged(Entity<ActorComponent> ent, ref BleedAmountChangedEvent args)
    {
        if (args.PreviousBleedAmount > 0 || args.NewBleedAmount <= 0)
            return;

        TryScream(ent);
    }

    private void OnBeforeDamageUserOnTrigger(Entity<DamageUserOnTriggerComponent> ent, ref BeforeDamageUserOnTriggerEvent args)
    {
        if (!TryComp<ActorComponent>(args.Tripper, out var actor))
            return;

        if (!IsPainfulHazard(ent))
            return;

        TryScream((args.Tripper, actor));
    }

    private bool IsPainfulHazard(Entity<DamageUserOnTriggerComponent> ent)
    {
        var prototypeId = MetaData(ent).EntityPrototype?.ID;
        if (prototypeId == null)
            return false;

        return prototypeId.Contains("Razorwire", StringComparison.OrdinalIgnoreCase)
            || prototypeId.Contains("Barbed", StringComparison.OrdinalIgnoreCase);
    }

    private void TryScream(Entity<ActorComponent> ent)
    {
        if (TryComp<MobStateComponent>(ent, out var mobState) && _mobState.IsDead(ent, mobState))
            return;

        var curTime = _timing.CurTime;
        if (_nextScreamTime.TryGetValue(ent, out var nextTime) && curTime < nextTime)
            return;

        _nextScreamTime[ent] = curTime + ScreamCooldown;
        _chat.TryEmoteWithChat(ent, "Scream");
    }
}