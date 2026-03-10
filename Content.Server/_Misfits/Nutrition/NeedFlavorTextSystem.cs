// #Misfits Change /Add/ - Private do-style hunger and thirst ambience for player characters.
using Content.Server.Chat.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.Nutrition;

/// <summary>
/// Sends first-person, private do-style flavor text to player-controlled entities
/// as hunger and thirst worsen, and occasionally while those states persist.
/// </summary>
public sealed class NeedFlavorTextSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, HungerThreshold> _lastHungerThresholds = new();
    private readonly Dictionary<EntityUid, ThirstThreshold> _lastThirstThresholds = new();
    private readonly Dictionary<(EntityUid Uid, string NeedId), TimeSpan> _nextAmbientAt = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextCollapseAttemptAt = new();

    private static readonly string[] HungerPeckishMessages =
    {
        "need-flavor-hunger-peckish-1",
        "need-flavor-hunger-peckish-2",
        "need-flavor-hunger-peckish-3",
        "need-flavor-hunger-peckish-4",
    };

    private static readonly string[] HungerStarvingMessages =
    {
        "need-flavor-hunger-starving-1",
        "need-flavor-hunger-starving-2",
        "need-flavor-hunger-starving-3",
        "need-flavor-hunger-starving-4",
    };

    private static readonly string[] ThirstThirstyMessages =
    {
        "need-flavor-thirst-thirsty-1",
        "need-flavor-thirst-thirsty-2",
        "need-flavor-thirst-thirsty-3",
        "need-flavor-thirst-thirsty-4",
    };

    private static readonly string[] ThirstParchedMessages =
    {
        "need-flavor-thirst-parched-1",
        "need-flavor-thirst-parched-2",
        "need-flavor-thirst-parched-3",
        "need-flavor-thirst-parched-4",
    };

    private static readonly string[] HungerDeadMessages =
    {
        "need-flavor-hunger-dead-1",
        "need-flavor-hunger-dead-2",
        "need-flavor-hunger-dead-3",
        "need-flavor-hunger-dead-4",
    };

    private static readonly string[] ThirstDeadMessages =
    {
        "need-flavor-thirst-dead-1",
        "need-flavor-thirst-dead-2",
        "need-flavor-thirst-dead-3",
        "need-flavor-thirst-dead-4",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerComponent, ComponentRemove>(OnHungerShutdown);
        SubscribeLocalEvent<ThirstComponent, ComponentShutdown>(OnThirstShutdown);
    }

    private void OnHungerShutdown(EntityUid uid, HungerComponent component, ComponentRemove args)
    {
        _lastHungerThresholds.Remove(uid);
        _nextAmbientAt.Remove((uid, "hunger"));
        _nextCollapseAttemptAt.Remove(uid);
    }

    private void OnThirstShutdown(EntityUid uid, ThirstComponent component, ComponentShutdown args)
    {
        _lastThirstThresholds.Remove(uid);
        _nextAmbientAt.Remove((uid, "thirst"));
        _nextCollapseAttemptAt.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var hungerQuery = EntityQueryEnumerator<ActorComponent, HungerComponent>();
        while (hungerQuery.MoveNext(out var uid, out var actor, out var hunger))
        {
            if (ShouldSkip(uid, actor.PlayerSession))
                continue;

            ProcessHunger(uid, actor.PlayerSession, hunger);
        }

        var thirstQuery = EntityQueryEnumerator<ActorComponent, ThirstComponent>();
        while (thirstQuery.MoveNext(out var uid, out var actor, out var thirst))
        {
            if (ShouldSkip(uid, actor.PlayerSession))
                continue;

            ProcessThirst(uid, actor.PlayerSession, thirst);
        }
    }

    private bool ShouldSkip(EntityUid uid, ICommonSession session)
    {
        if (session.AttachedEntity != uid)
            return true;

        return TryComp<MobStateComponent>(uid, out var mobState) && _mobState.IsDead(uid, mobState);
    }

    private void ProcessHunger(EntityUid uid, ICommonSession session, HungerComponent hunger)
    {
        var threshold = hunger.CurrentThreshold;
        if (!_lastHungerThresholds.TryAdd(uid, threshold))
        {
            var previous = _lastHungerThresholds[uid];
            if (previous != threshold)
            {
                _lastHungerThresholds[uid] = threshold;

                if (IsInterestingHungerThreshold(threshold))
                    SendAmbientNeedMessage(uid, session, "hunger", GetHungerMessages(threshold), GetHungerCooldown(threshold));

                return;
            }
        }

        if (!IsInterestingHungerThreshold(threshold))
            return;

        if (threshold == HungerThreshold.Dead)
            TryCauseCollapse(uid);

        if (CanSendAmbient(uid, "hunger"))
            SendAmbientNeedMessage(uid, session, "hunger", GetHungerMessages(threshold), GetHungerCooldown(threshold));
    }

    private void ProcessThirst(EntityUid uid, ICommonSession session, ThirstComponent thirst)
    {
        var threshold = thirst.CurrentThirstThreshold;
        if (!_lastThirstThresholds.TryAdd(uid, threshold))
        {
            var previous = _lastThirstThresholds[uid];
            if (previous != threshold)
            {
                _lastThirstThresholds[uid] = threshold;

                if (IsInterestingThirstThreshold(threshold))
                    SendAmbientNeedMessage(uid, session, "thirst", GetThirstMessages(threshold), GetThirstCooldown(threshold));

                return;
            }
        }

        if (!IsInterestingThirstThreshold(threshold))
            return;

        if (threshold == ThirstThreshold.Dead)
            TryCauseCollapse(uid);

        if (CanSendAmbient(uid, "thirst"))
            SendAmbientNeedMessage(uid, session, "thirst", GetThirstMessages(threshold), GetThirstCooldown(threshold));
    }

    private void TryCauseCollapse(EntityUid uid)
    {
        if (_nextCollapseAttemptAt.TryGetValue(uid, out var nextAt) && _timing.CurTime < nextAt)
            return;

        _nextCollapseAttemptAt[uid] = _timing.CurTime + TimeSpan.FromSeconds(18);

        if (_random.Prob(0.18f))
            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2.5f), true);
    }

    private bool CanSendAmbient(EntityUid uid, string needId)
    {
        return !_nextAmbientAt.TryGetValue((uid, needId), out var nextAt) || _timing.CurTime >= nextAt;
    }

    private void SendAmbientNeedMessage(EntityUid uid, ICommonSession session, string needId, string[] messageKeys, TimeSpan cooldown)
    {
        _nextAmbientAt[(uid, needId)] = _timing.CurTime + cooldown;
        var text = Loc.GetString(_random.Pick(messageKeys));
        _chat.SendPrivateDoMessage(session, text);
    }

    private static bool IsInterestingHungerThreshold(HungerThreshold threshold)
    {
        return threshold is HungerThreshold.Peckish or HungerThreshold.Starving or HungerThreshold.Dead;
    }

    private static bool IsInterestingThirstThreshold(ThirstThreshold threshold)
    {
        return threshold is ThirstThreshold.Thirsty or ThirstThreshold.Parched or ThirstThreshold.Dead;
    }

    private static string[] GetHungerMessages(HungerThreshold threshold)
    {
        return threshold switch
        {
            HungerThreshold.Dead => HungerDeadMessages,
            HungerThreshold.Starving => HungerStarvingMessages,
            HungerThreshold.Peckish => HungerPeckishMessages,
            _ => Array.Empty<string>(),
        };
    }

    private static string[] GetThirstMessages(ThirstThreshold threshold)
    {
        return threshold switch
        {
            ThirstThreshold.Dead => ThirstDeadMessages,
            ThirstThreshold.Parched => ThirstParchedMessages,
            ThirstThreshold.Thirsty => ThirstThirstyMessages,
            _ => Array.Empty<string>(),
        };
    }

    private static TimeSpan GetHungerCooldown(HungerThreshold threshold)
    {
        return threshold switch
        {
            HungerThreshold.Dead => TimeSpan.FromSeconds(45),
            HungerThreshold.Starving => TimeSpan.FromSeconds(70),
            HungerThreshold.Peckish => TimeSpan.FromSeconds(140),
            _ => TimeSpan.FromSeconds(120),
        };
    }

    private static TimeSpan GetThirstCooldown(ThirstThreshold threshold)
    {
        return threshold switch
        {
            ThirstThreshold.Dead => TimeSpan.FromSeconds(40),
            ThirstThreshold.Parched => TimeSpan.FromSeconds(55),
            ThirstThreshold.Thirsty => TimeSpan.FromSeconds(110),
            _ => TimeSpan.FromSeconds(90),
        };
    }
}