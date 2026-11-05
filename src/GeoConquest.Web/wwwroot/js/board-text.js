// The board's wording in one place. The labels at the top mirror BoardText.cs, which prints the page before this
// script runs, so a change to one must be made in the other.

export const plural = (count, noun) => (count === 1 ? `1 ${noun}` : `${count} ${noun}s`);

export const countryCount = (count) => (count === 1 ? "1 country" : `${count} countries`);

const PHASES = {
    Reinforce: "Placing reinforcements",
    Action: "Choosing an action",
    Attacking: "Attack in progress",
    PostAttack: "After the attack",
};

export const phaseLabel = (phase) => PHASES[phase] ?? "Game over";

export function resultTitle(status) {
    return status === "Won" ? "Victory" : status === "Lost" ? "Defeat" : "Game over";
}

export function playerLabel(player, viewerIsOwner) {
    if (!player.isHuman) {
        return `${player.name} (NPC)`;
    }

    return viewerIsOwner ? `${player.name} (you)` : `${player.name} (player)`;
}

export function turnTitle(state) {
    if (state.status !== "InProgress") {
        return "Game over";
    }

    const current = state.players.find((player) => player.id === state.currentPlayerId);
    return current.isHuman && state.viewerIsOwner ? "Your turn" : `${playerLabel(current, state.viewerIsOwner)} is playing`;
}

/** A win chance as a whole percentage, never claiming certainty the odds do not have. */
export function formatChance(probability) {
    const percent = Math.round(probability * 100);
    if (probability > 0 && percent < 1) {
        return "under 1%";
    }

    return probability < 1 && percent > 99 ? "over 99%" : `${percent}%`;
}

export function joinDice(values) {
    return values.length < 2 ? values.join("") : `${values.slice(0, -1).join(", ")} and ${values.at(-1)}`;
}

/** The round in words, from the dice values in the event, so the dice tiles are never the only record. */
export function describeRoll(roll) {
    return `Attacker rolled ${joinDice(roll.attackerDice)}; defender rolled ${joinDice(roll.defenderDice)}. ` +
        `Attacker loses ${roll.attackerLosses}, defender loses ${roll.defenderLosses}.`;
}

const BATTLE_TYPES = ["AttackStarted", "DiceRolled", "AttackStopped", "AttackFailed", "CountryConquered", "PlayerEliminated"];

/** Which log filter an event belongs to. */
export function eventKind(type) {
    if (type === "NpcDecision") {
        return "npc";
    }

    if (type === "Commentary") {
        return "commentary";
    }

    return BATTLE_TYPES.includes(type) ? "battle" : "other";
}

/** Model text is untrusted, so it only ever reaches the page as textContent. The label says who wrote the line. */
export function describeCommentary(gameEvent) {
    return `${gameEvent.commentarySource === "Ai" ? "AI commentary" : "Template commentary"}: ${gameEvent.message}`;
}

/** An NPC's reasoning carries its name, so it reads as their explanation and not as a game result. */
export function describeEvent(gameEvent, state) {
    if (gameEvent.type === "Commentary") {
        return describeCommentary(gameEvent);
    }

    const npc = gameEvent.type === "NpcDecision" ? state.players.find((player) => player.id === gameEvent.actorPlayerId) : null;
    return npc ? `${npc.name} (NPC) explains: ${gameEvent.message}` : gameEvent.message;
}
