// #Misfits Add: Broadcasts an emote chat message to nearby players when a player points at an entity.
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Pointing;
using Robust.Shared.GameObjects;

namespace Content.Server._Misfits.Pointing;

/// <summary>
/// Hooks <see cref="AfterPointedAtEvent"/> to send a local-area emote chat message so nearby
/// players see "* John Smith points at Jane Doe *" in the emote channel whenever someone points.
/// </summary>
public sealed class PointingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to the event raised on the pointer entity after a successful point.
        SubscribeLocalEvent<MetaDataComponent, AfterPointedAtEvent>(OnAfterPointed);
    }

    /// <summary>
    /// Sends an emote chat message when a player points at another entity.
    /// Bypasses the action blocker because pointing is already gated by PointingSystem.
    /// </summary>
    private void OnAfterPointed(EntityUid uid, MetaDataComponent component, ref AfterPointedAtEvent ev)
    {
        // Resolve the display name of the entity being pointed at.
        var pointedName = Identity.Entity(ev.Pointed, EntityManager);

        // The message text; the emote system wraps it as "* <name> <message> *" in chat.
        var message = Loc.GetString("pointing-chat-point-at-other", ("other", pointedName));

        // Send as a local-area emote — visible to nearby players in the Emotes channel.
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Emote,
            ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }
}
