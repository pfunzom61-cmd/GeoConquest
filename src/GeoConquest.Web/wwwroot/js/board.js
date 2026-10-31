// Draws the game board and posts the owner's intents. The server decides everything: this script never rolls dice or
// checks rules, it only shows the state and events the server returns. All text goes in through textContent.
// While the NPCs play, the server pushes their moves (board-live.js); the board reads the same data whether it
// arrives by push, by an action's response, or by asking for a sync.

import { hasMissedEvents, mergeEvents, syncCursor } from "./board-events.js";
import { startLiveUpdates } from "./board-live.js";

// Long enough for the commentary on the game's last roll to arrive after the game has ended.
const GAME_OVER_LISTEN_MS = 5000;

const LIVE_STATUS_TEXT = {
    connecting: "Live updates: connecting…",
    live: "Live updates: connected",
    reconnecting: "Live updates: connection lost, reconnecting… (checking for news every few seconds meanwhile)",
    polling: "Live updates: unavailable, checking for news every few seconds instead",
};

const pageData = JSON.parse(document.getElementById("board-data").textContent);
const requestToken = document.querySelector('meta[name="request-verification-token"]')?.content ?? "";
const root = document.getElementById("board");

let state = pageData.state;
let events = pageData.recentEvents;
let busy = false;
let errorMessage = "";
let liveStatus = "connecting";
let syncing = false;
let syncAgain = false;
let live = null;
let stopListeningTimer = null;

/** Adds events not seen before. Returns true if any were new. */
function addEvents(incoming) {
    const merged = mergeEvents(events, incoming);
    const changed = merged !== events;
    events = merged;
    return changed;
}

/** State from the server's answer to this viewer (an action or a sync). Older versions are ignored. */
function applyOwnState(incoming) {
    if (incoming.version < state.version) {
        return false;
    }

    const changed = incoming.version !== state.version || incoming.viewerIsOwner !== state.viewerIsOwner;
    state = incoming;
    return changed;
}

/**
 * State pushed to everyone. It is the spectator's view (no actions), so the owner keeps their own role and fetches
 * their actions with a sync when the turn reaches them.
 */
function onLiveState(incoming) {
    if (incoming.version <= state.version) {
        return;
    }

    if (state.viewerIsOwner && incoming.status === "InProgress" && incoming.currentPlayerId === humanPlayerId()) {
        // The owner's turn: the pushed state has no actions in it, so fetch the owner's own view instead of showing it.
        sync();
        return;
    }

    state = { ...incoming, viewerIsOwner: state.viewerIsOwner };
    render();
    afterStateChange();
}

function onLiveEvents(incoming) {
    if (hasMissedEvents(events, incoming)) {
        sync(); // a message was missed, so ask for everything after what we have
        return;
    }

    if (addEvents(incoming)) {
        refreshLogs();
    }
}

/** Loads the latest state and any events missed. Used on connect, after a dropped connection, on conflicts, and when polling. */
async function sync() {
    if (syncing) {
        syncAgain = true;
        return;
    }

    syncing = true;
    try {
        do {
            syncAgain = false;
            const response = await fetch(`/api/games/${state.gameId}/sync?afterSequence=${syncCursor(events, Date.now())}`);
            if (response.ok) {
                const result = await response.json();
                const stateChanged = applyOwnState(result.state);
                const eventsChanged = addEvents(result.newEvents);
                if (stateChanged) {
                    render();
                    afterStateChange();
                } else if (eventsChanged) {
                    refreshLogs();
                }
            }
        } while (syncAgain);
    } finally {
        syncing = false;
    }
}

/** Once the game is over only the last roll's commentary can still arrive, so stop listening shortly afterwards. */
function afterStateChange() {
    if (state.status !== "InProgress") {
        stopListeningTimer ??= setTimeout(() => live?.stop(), GAME_OVER_LISTEN_MS);
    }
}

function humanPlayerId() {
    return state.players.find((player) => player.isHuman).id;
}

function playerName(playerId) {
    return state.players.find((player) => player.id === playerId)?.displayName ?? "";
}

function el(tag, props = {}, ...children) {
    const node = document.createElement(tag);
    for (const [name, value] of Object.entries(props)) {
        if (name.startsWith("aria-")) {
            node.setAttribute(name, value);
        } else {
            node[name] = value;
        }
    }

    node.append(...children);
    return node;
}

function labelled(text, control) {
    return el("div", {}, el("label", { htmlFor: control.id, textContent: text }), " ", control);
}

function ownedTerritories() {
    const humanId = humanPlayerId();
    return state.territories.filter((territory) => territory.ownerPlayerId === humanId);
}

function territorySelect(id, territories) {
    return el("select", { id }, ...territories.map((territory) =>
        el("option", { value: territory.countryCode, textContent: `${territory.countryName} (${territory.soldiers})` })));
}

async function send(action, body) {
    if (busy) {
        return;
    }

    busy = true;
    errorMessage = "";
    render();

    try {
        const response = await fetch(`/api/games/${state.gameId}/${action}`, {
            method: "POST",
            headers: { "Content-Type": "application/json", RequestVerificationToken: requestToken },
            body: body === undefined ? undefined : JSON.stringify(body),
        });
        const result = await response.json().catch(() => ({}));

        if (response.ok) {
            applyOwnState(result.state);
            addEvents(result.newEvents);
            afterStateChange();
        } else {
            errorMessage = result.error ?? "Something went wrong. Please try again.";
            if (response.status === 409) {
                await sync();
            }
        }
    } catch {
        errorMessage = "Could not reach the server. Check your connection and try again.";
    }

    busy = false;
    render();
}

function actionButton(text, action, onClick) {
    const allowed = state.availableActions.includes(action);
    const button = el("button", { type: "button", textContent: text, disabled: busy || !allowed });
    if (!allowed) {
        button.title = `Not available now. ${state.nextStep}`;
    }
    button.addEventListener("click", onClick);
    return button;
}

function buildBanner() {
    const banner = el("section", { "aria-label": "Turn" },
        el("h2", { textContent: `Round ${state.round}: ${state.status === "InProgress" ? `${state.currentPlayerName}'s turn` : "Game over"}` }),
        el("p", { role: "status", textContent: state.nextStep }),
        el("p", { textContent: `Phase: ${state.phase}` }));

    if (state.isFastForward) {
        banner.append(el("p", { textContent: "Fast-forward is on: the NPC turns are playing at a quicker pace." }));
    }

    // Written out, not just coloured, so the connection state is clear to everyone.
    banner.append(el("p", { id: "live-status", textContent: LIVE_STATUS_TEXT[liveStatus] }));
    return banner;
}

function buildFastForwardControl() {
    const turnPending = state.currentPlayerId === humanPlayerId();
    const toggle = el("button", {
        type: "button",
        textContent: state.isFastForward ? "Turn fast-forward off" : "Fast-forward NPC turns",
        disabled: busy || state.status !== "InProgress",
    });
    if (state.status !== "InProgress") {
        toggle.title = "The game is over, so there is nothing left to speed up.";
    }

    toggle.addEventListener("click", () => send("fast-forward", { enabled: !state.isFastForward }));

    return el("fieldset", {}, el("legend", { textContent: "NPC pace" }),
        toggle,
        el("p", { textContent: turnPending
            ? "Applies to the NPC turns after you end your turn. It switches itself off when your turn comes round again."
            : "It switches itself off when your turn comes round again." }));
}

/** The latest attack, round by round, drawn from the events so a spectator sees exactly what the owner sees. */
function buildBattleLog() {
    const startIndex = events.map((gameEvent) => gameEvent.type).lastIndexOf("AttackStarted");
    const section = el("section", { id: "battle-log", "aria-label": "Latest battle" }, el("h2", { textContent: "Latest battle" }));

    if (startIndex < 0) {
        section.append(el("p", { textContent: "No attack has been made yet." }));
        return section;
    }

    section.append(el("p", { textContent: events[startIndex].message }));
    const rounds = events.slice(startIndex + 1)
        .filter((gameEvent) => gameEvent.type === "DiceRolled")
        .map((gameEvent, index) => {
            const round = el("li", {
                textContent: `Round ${index + 1}: attacker rolled ${gameEvent.attackerDice.join(", ")}; defender rolled ${gameEvent.defenderDice.join(", ")}. ` +
                    `Attacker loses ${gameEvent.attackerLosses}, defender loses ${gameEvent.defenderLosses}.`,
            });
            const commentary = events.find((candidate) => candidate.type === "Commentary" && candidate.sequence === gameEvent.sequence + 1);
            if (commentary) {
                round.append(el("p", { textContent: describeCommentary(commentary) }));
            }
            return round;
        });
    section.append(el("ol", {}, ...rounds));
    return section;
}

function buildTerritories() {
    const rows = state.territories.map((territory) => el("tr", {},
        el("th", { scope: "row", textContent: territory.countryName }),
        el("td", { textContent: territory.ownerName }),
        el("td", { textContent: String(territory.soldiers) }),
        el("td", { textContent: String(territory.soldierValue) })));

    return el("section", { "aria-label": "Countries" },
        el("h2", { textContent: "Countries" }),
        el("table", {},
            el("thead", {}, el("tr", {},
                ...["Country", "Owner", "Soldiers", "Value"].map((heading) => el("th", { scope: "col", textContent: heading })))),
            el("tbody", {}, ...rows)));
}

function buildReinforceForm() {
    const inputs = ownedTerritories().map((territory) => {
        const input = el("input", { id: `place-${territory.countryCode}`, type: "number", min: 0, value: 0 });
        input.dataset.countryCode = territory.countryCode;
        return labelled(`${territory.countryName} (${territory.soldiers})`, input);
    });

    const submit = actionButton(`Place reinforcements (${state.pendingReinforcements} to place)`, "allocate", () => {
        const allocations = {};
        for (const input of form.querySelectorAll("input[type=number]")) {
            if (Number(input.value) > 0) {
                allocations[input.dataset.countryCode] = Number(input.value);
            }
        }
        send("allocate", { allocations });
    });

    const form = el("fieldset", {}, el("legend", { textContent: "Reinforcements" }), ...inputs, submit);
    return form;
}

function buildTransferForm() {
    const owned = ownedTerritories();
    const from = territorySelect("transfer-from", owned);
    const to = territorySelect("transfer-to", owned);
    const count = el("input", { id: "transfer-count", type: "number", min: 1, value: 1 });

    const submit = actionButton("Move soldiers", "transfer", () =>
        send("transfer", { fromCountryCode: from.value, toCountryCode: to.value, soldiers: Number(count.value) }));

    return el("fieldset", {}, el("legend", { textContent: "Move soldiers" }),
        labelled("From", from), labelled("To", to), labelled("Soldiers", count), submit);
}

function buildAttackForm() {
    const owned = ownedTerritories();
    const attackers = owned.filter((territory) => territory.soldiers >= 2);
    const targets = state.territories.filter((territory) => !owned.includes(territory));
    const from = territorySelect("attack-from", attackers);
    const target = territorySelect("attack-target", targets);

    const submit = actionButton("Start attack", "attack", () =>
        send("attack", { attackerCountryCode: from.value, defenderCountryCode: target.value }));
    if (attackers.length === 0 && state.availableActions.includes("attack")) {
        submit.disabled = true;
        submit.title = "None of your countries has the 2 soldiers needed to attack.";
    }

    return el("fieldset", {}, el("legend", { textContent: "Attack" }),
        labelled("Attack from", from), labelled("Target", target), submit);
}

function buildBattleControls() {
    const battle = state.battle;
    const summary = battle
        ? `${battle.attackerName} is attacking ${battle.defenderName} (${battle.roundsFought} rounds fought).`
        : "No attack in progress.";

    const stop = actionButton("Stop attack and end turn", "stop", () => {
        if (confirm("Stopping the attack ends your turn. Continue?")) {
            send("stop");
        }
    });

    return el("fieldset", {}, el("legend", { textContent: "Battle" }),
        el("p", { textContent: summary }), actionButton("Roll dice", "roll", () => send("roll")), " ", stop);
}

function buildTurnControls() {
    const skip = actionButton("Skip turn", "skip", () => {
        if (confirm("Skip your turn without attacking?")) {
            send("skip");
        }
    });

    return el("fieldset", {}, el("legend", { textContent: "End of turn" }),
        skip, " ", actionButton("End turn", "endTurn", () => send("end-turn")));
}

function buildActionPanel() {
    if (!state.viewerIsOwner) {
        return el("p", { textContent: "Spectating: you can watch every move but not play." });
    }

    const panel = el("section", { "aria-label": "Your actions" }, el("h2", { textContent: "Your actions" }));
    if (errorMessage) {
        panel.append(el("p", { role: "alert", className: "error", textContent: errorMessage }));
    }

    panel.append(buildReinforceForm(), buildTransferForm(), buildAttackForm(), buildBattleControls(), buildTurnControls(), buildFastForwardControl());
    return panel;
}

/** Model text is untrusted, so it only ever reaches the page as textContent. The label says who wrote the line. */
function describeCommentary(gameEvent) {
    return `${gameEvent.commentarySource === "Ai" ? "AI commentary" : "Template commentary"}: ${gameEvent.message}`;
}

function describeEvent(gameEvent) {
    if (gameEvent.type === "Commentary") {
        return `#${gameEvent.sequence} ${describeCommentary(gameEvent)}`;
    }

    // An NPC's reasoning is shown with its name so it reads as their explanation, not as a game result.
    const npcName = gameEvent.type === "NpcDecision" ? playerName(gameEvent.actorPlayerId) : "";
    return npcName ? `#${gameEvent.sequence} ${npcName} (NPC) explains: ${gameEvent.message}` : `#${gameEvent.sequence} ${gameEvent.message}`;
}

function buildEventLog() {
    const entries = [...events].reverse().map((gameEvent) => el("li", { textContent: describeEvent(gameEvent) }));

    return el("section", { id: "event-log", "aria-label": "Event log" },
        el("h2", { textContent: "Event log (latest first)" }),
        el("ul", {}, ...entries));
}

/** Redraws only the logs. New events can arrive at any moment, and must not wipe a form the owner is filling in. */
function refreshLogs() {
    document.getElementById("battle-log")?.replaceWith(buildBattleLog());
    document.getElementById("event-log")?.replaceWith(buildEventLog());
}

function render() {
    // Keep the keyboard where it was: a re-render replaces the controls, so remember which one had focus.
    const focusedId = document.activeElement?.id;
    root.replaceChildren(buildBanner(), buildTerritories(), buildActionPanel(), buildBattleLog(), buildEventLog());
    if (focusedId) {
        document.getElementById(focusedId)?.focus();
    }
}

render();

if (state.status === "InProgress") {
    live = startLiveUpdates(state.gameId, {
        onEvents: onLiveEvents,
        onState: onLiveState,
        onStatus: (status) => {
            liveStatus = status;
            render();
        },
        sync,
    });
}
