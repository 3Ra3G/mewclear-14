// #Misfits Change - Route players to faction-specific objective pools (NCRObjectives / BOSWObjectives)
// to eliminate mismatched-requirement warnings caused by the shared N14Objectives pool.
// #Misfits Add - Non-faction players (wastelanders, civilians, etc.) receive the SurviveObjective instead.
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class N14RuleSystem : GameRuleSystem<N14RuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    // Faction prototype IDs used for per-faction objective routing
    private static readonly ProtoId<NpcFactionPrototype> NCRFaction = "NCR";
    private static readonly ProtoId<NpcFactionPrototype> BOSWFaction = "BrotherhoodOfSteel";
    private static readonly ProtoId<NpcFactionPrototype> LegionFaction = "CaesarLegion"; // #Misfits Change

    // #Misfits Add - Survive objective prototype ID for non-faction players
    private const string SurviveObjectiveId = "SurviveObjective";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var query = EntityQueryEnumerator<N14RuleComponent>();
        while (query.MoveNext(out var uid, out var rule))
        {
            if (_mindSystem.TryGetMind(args.Player, out var mindId, out var mind))
            {
                // Don't add more objectives if the player already has some (e.g. respawning)
                if (mind.Objectives.Count > 0)
                    return;

                var objectiveGroup = GetObjectiveGroupForFaction(mindId, mind);

                if (objectiveGroup == null)
                {
                    // #Misfits Add - No known faction → give the universal Survive objective
                    var surviveObjective = _objectives.TryCreateObjective(mindId, mind, SurviveObjectiveId);
                    if (surviveObjective != null)
                    {
                        Logger.DebugS("n14rule", $"Added SurviveObjective for non-faction player {args.Player}");
                        _mindSystem.AddObjective(mindId, mind, surviveObjective.Value);
                    }
                    else
                    {
                        Logger.DebugS("n14rule", $"Could not create SurviveObjective for {args.Player}");
                    }
                }
                else
                {
                    // Pick the objective pool that matches this player's faction to avoid requirement mismatches
                    var objective = _objectives.GetRandomObjective(mindId, mind, objectiveGroup);
                    if (objective != null)
                    {
                        Logger.DebugS("n14rule", $"Added objective {objective.Value} for {args.Player}");
                        _mindSystem.AddObjective(mindId, mind, objective.Value);
                    }
                    else
                    {
                        Logger.DebugS("n14rule", $"No suitable objectives found for {args.Player}");
                    }
                }
            }
            else
            {
                Logger.DebugS("n14rule", $"{args.Player} has no mind");
            }

            // break out of loop: we only need to do this once
            break;
        }
    }

    /// <summary>
    /// Returns the WeightedRandom objective group prototype ID matched to a player's NPC faction,
    /// or <c>null</c> when the player belongs to no known major faction (wastelander / civilian).
    /// </summary>
    private string? GetObjectiveGroupForFaction(EntityUid mindId, MindComponent mind)
    {
        if (_mindSystem.InFaction(mindId, mind, new HashSet<ProtoId<NpcFactionPrototype>> { NCRFaction }))
            return "NCRObjectives";

        if (_mindSystem.InFaction(mindId, mind, new HashSet<ProtoId<NpcFactionPrototype>> { BOSWFaction }))
            return "BOSWObjectives";

        // #Misfits Change - Route Legion players to their own objective pool
        if (_mindSystem.InFaction(mindId, mind, new HashSet<ProtoId<NpcFactionPrototype>> { LegionFaction }))
            return "LegionObjectives";

        // #Misfits Add - Return null so the caller can assign the Survive objective instead
        return null;
    }
}
