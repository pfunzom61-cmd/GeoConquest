// The battle arena (the latest attack, with its dice) and the commentary strip beneath it. Everything is drawn from the
// events, so a spectator sees exactly what the owner sees. The dice tiles are decoration: the sentence beside them says
// the same in words.

import { setText } from "./dom.js";
import { describeCommentary, describeRoll } from "./board-text.js";
import { prefersReducedMotion } from "./motion.js";

const MAX_PAIRS_SHOWN = 6; // an attack can roll dozens of dice; the sentence still gives every value
const ROLL_SETTLE_MS = 1000; // the most the counters wait for the dice

/** Who won each pairing: the higher die wins, a tie wins nothing (the battle rule is the server's; this only labels it). */
function pairResults(attackerDice, defenderDice) {
    return attackerDice.map((attackerDie, index) => {
        const defenderDie = defenderDice[index];
        return attackerDie === defenderDie
            ? ["tie", "tie"]
            : attackerDie > defenderDie ? ["won", "lost"] : ["lost", "won"];
    });
}

function eventsOfLatestAttack(events) {
    const startIndex = events.map((gameEvent) => gameEvent.type).lastIndexOf("AttackStarted");
    return startIndex < 0 ? null : { started: events[startIndex], after: events.slice(startIndex + 1) };
}

const OUTCOME_TYPES = ["CountryConquered", "AttackFailed", "AttackStopped"];

export function createArena(arena, ticker) {
    const template = arena.querySelector("[data-die-template]");
    const sides = arena.querySelector("[data-arena-sides]");
    let shownRollSequence = 0;

    function drawDice(side, values, results, tumble) {
        const list = arena.querySelector(`[data-dice="${side}"]`);
        const dice = values.slice(0, MAX_PAIRS_SHOWN).map((value, index) => {
            const item = template.content.firstElementChild.cloneNode(true);
            const die = item.querySelector(".gc-die");
            die.dataset.value = String(value);
            item.dataset.result = results[index];
            item.querySelector(".gc-dice__result").textContent = { won: "Won", lost: "Lost", tie: "Tie" }[results[index]];
            if (tumble) {
                die.classList.add("gc-die--tumble");
                die.addEventListener("animationend", () => die.classList.remove("gc-die--tumble"), { once: true });
            }

            return item;
        });

        list.replaceChildren(...dice);
    }

    /**
     * Draws the latest attack. A roll the board has not shown before tumbles, unless the player has asked for
     * reduced motion or fast-forward is on. Returns how long the counters should wait for the dice (0 for none).
     */
    function showBattle(events, state, animate) {
        const attack = eventsOfLatestAttack(events);
        if (!attack) {
            return 0;
        }

        const rolls = attack.after.filter((gameEvent) => gameEvent.type === "DiceRolled");
        const latest = rolls.at(-1);
        const outcome = attack.after.find((gameEvent) => OUTCOME_TYPES.includes(gameEvent.type));
        const summary = [attack.started.message, outcome?.message].filter(Boolean).join(" ");
        setText(arena, "arena-summary", summary);
        sides.hidden = !latest;

        if (!latest) {
            setText(arena, "arena-result", "");
            return 0;
        }

        const isNew = latest.sequence > shownRollSequence;
        const tumble = isNew && animate && !prefersReducedMotion() && !state.isFastForward;
        const pairs = pairResults(latest.attackerDice, latest.defenderDice);

        shownRollSequence = Math.max(shownRollSequence, latest.sequence);
        setText(arena, "attacker-title", `Attacker: ${nameOf(state, attack.started.fromCountryCode)}`);
        setText(arena, "defender-title", `Defender: ${nameOf(state, attack.started.toCountryCode)}`);
        drawDice("attacker", latest.attackerDice, pairs.map((pair) => pair[0]), tumble);
        drawDice("defender", latest.defenderDice, pairs.map((pair) => pair[1]), tumble);

        const extra = Math.max(0, latest.attackerDice.length - MAX_PAIRS_SHOWN);
        setText(arena, "arena-result", `Round ${rolls.length}. ${describeRoll(latest)}${extra ? ` Only the first ${MAX_PAIRS_SHOWN} pairs of dice are drawn.` : ""}`);
        return tumble ? ROLL_SETTLE_MS : 0;
    }

    /** The newest commentary line, labelled as AI or template. The strip is the board's only live region. */
    function showCommentary(events) {
        const latest = events.findLast((gameEvent) => gameEvent.type === "Commentary");
        if (!latest) {
            return;
        }

        const [label, ...text] = describeCommentary(latest).split(": ");
        setText(ticker, "ticker-text", text.join(": "));
        setText(ticker, "ticker-source", label);
    }

    return { showBattle, showCommentary };
}

function nameOf(state, countryCode) {
    return state.territories.find((territory) => territory.countryCode === countryCode)?.countryName ?? countryCode;
}
