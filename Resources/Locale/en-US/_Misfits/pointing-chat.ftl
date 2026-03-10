## Misfits Chat Action Broadcasting — #Misfits Add
# Emote chat text broadcast for player interactions that normally only show as sprite popups.
# All strings are the action portion only; the emote system wraps them as: "* <name> <message> *"

## PointingChatSystem
pointing-chat-point-at-other = points at {$other}

## OfferItemSystem
# Broadcast when a player hands an item to another player after accepting an offer.
misfits-chat-offer-handoff = hands {$item} to {$target}

## CarryingSystem
# Broadcast when a player picks up or puts down another entity.
misfits-chat-carry-pickup = picks up {$carried}
misfits-chat-carry-drop = puts down {$carried}

## CuffingChatSystem
# Broadcast when a player successfully restrains another entity with handcuffs.
misfits-chat-cuff-applied = restrains {$target}
misfits-chat-cuff-self = restrains themselves
