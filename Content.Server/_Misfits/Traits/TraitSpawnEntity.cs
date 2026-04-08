using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

// #Misfits Add - TraitSpawnEntity: spawns a separate entity at the player's position when the trait activates.
// Used for pet companion traits so each pet gets its own ghost-role spawner entity,
// avoiding the component-duplication limitation of TraitAddComponent.

namespace Content.Server._Misfits.Traits;

/// <summary>
///     Spawns one or more entities at the player's location when the trait is applied.
///     Unlike <c>TraitAddComponent</c>, each entity is independent, so multiple traits
///     can each spawn their own ghost-role spawner without component conflicts.
/// </summary>
[UsedImplicitly]
public sealed partial class TraitSpawnEntity : TraitFunction
{
    /// <summary>
    ///     Prototype IDs to spawn at the player's coordinates.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Prototypes { get; private set; } = new();

    public override void OnPlayerSpawn(
        EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager)
    {
        // Resolve the player's current map coordinates for spawning
        var xform = entityManager.GetComponent<TransformComponent>(uid);
        var coords = xform.Coordinates;

        foreach (var proto in Prototypes)
        {
            entityManager.SpawnEntity(proto, coords);
        }
    }
}
