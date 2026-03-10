# Misfits Change — Addiction withdrawal popup messages and guidebook text

# Withdrawal effect popups (1-indexed for localizedDataset prefix+count)
addiction-effect-1 = Your hands are shaking uncontrollably.
addiction-effect-2 = You feel a desperate craving gnawing at your insides.
addiction-effect-3 = A cold sweat breaks out across your skin.
addiction-effect-4 = Your muscles ache and twitch painfully.
addiction-effect-5 = You feel nauseous and lightheaded.
addiction-effect-6 = An unbearable itch crawls beneath your skin.
addiction-effect-7 = Your head pounds with a splitting headache.
addiction-effect-8 = You can't stop thinking about your next fix.
addiction-effect-9 = Your stomach churns violently.
addiction-effect-10 = You feel weak and irritable.

# Misfits Change /Add:/ Drug-specific addiction chat messages
# These replace the generic popup with a named-drug message sent to the player's chat.
# Severity tiers are based on DoseCount: new (first), grows (2-3), deepens (4-7), severe (8+).
addiction-drug-first = You feel your body beginning to crave { $drug }.
addiction-drug-grows = Your addiction to { $drug } grows.
addiction-drug-deepens = Your need for { $drug } is deepening.
addiction-drug-severe = { $drug } has a powerful grip on you.

# Misfits Change /Add:/ Fading messages — sent once each time the remaining addiction tier drops.
# Tier 3 (severe) > 120s, tier 2 (moderate) 60-120s, tier 1 (mild) 15-60s, tier 0 (nearly gone) <15s.
addiction-drug-fading-moderate = The worst of your { $drug } craving is beginning to pass.
addiction-drug-fading-mild     = Your need for { $drug } is weakening.
addiction-drug-fading-nearly   = Your craving for { $drug } is almost gone.
addiction-drug-clean           = The last of your { $drug } craving fades away.

# Guidebook descriptions
reagent-effect-guidebook-addicted = { $chance ->
    [1] Causes
    *[other] cause
} an addiction
reagent-effect-guidebook-addiction-suppression = { $chance ->
    [1] Suppresses
    *[other] suppress
} active addictions
