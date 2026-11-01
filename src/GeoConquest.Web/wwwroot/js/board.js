// Runs the game board. The server decides everything: this script never rolls dice or checks rules, it only shows the
// state and events the server returns and posts the owner's intents. The page itself is drawn by the server; the
// modules imported here patch it. While the NPCs play the server pushes their moves (board-live.js), and the board reads
// the same data whether it arrives by push, by an action's response, or by asking for a sync.

import { hasMissedEvents, mergeEvents, syncCursor } from "./board-events.js";
import { startLiveUpdates } from "./board-live.js";
import { createCards } from "./board-cards.js";
import { createArena } from "./board-arena.js";
import { createLog } from "./board-log.js";
import { createOrders } from "./board-orders.js";
import { patchBanner, patchGameOver, patchLiveStatus, patchPlayers } from "./board-status.js";
import { startShare } from "./share.js";
import { showToast } from "./toast.js";

// Long enough for the commentary on the game's last roll to arrive after the game has ended.
const GAME_OVER_LISTEN_MS = 5000;
// A roll newer than this is a live one worth tumbling; older ones are history shown at rest.
const LIVE_ROLL_MAX_AGE_MS = 15000;

// The same verb as the button that was pressed.
const DONE_TEXT = {
    allocate: "Reinforcements placed",
    transfer: "Soldiers moved",
    attack: "Attack started",
    roll: "Dice rolled",
    stop: "Attack stopped",
    skip: "Turn skipped",
    "end-turn": "Turn ended",
};

const pageData = JSON.parse(document.getElementById("board-data").textContent);
const requestToken = document.querySelector('meta[name="request-verification-token"]')?.content ?? "";
const board = document.querySelector("[data-board]");

let state = pageData.state;
let events = pageData.recentEvents;
let busy = false;
let errorMessage = "";
let syncing = false;
let syncAgain = false;
let live = null;
let stopListeningTimer = null;
let holdUntil = 0; // the counters wait for tumbling dice until this time, never longer
let renderTimer = null;
let wasPlayersTurn = state.canAct;

const cards = createCards(document.querySelector("[data-map]"), state.viewerIsOwner);
const arena = createArena(document.querySelector("[data-arena]"), document.querySelector("[data-ticker]"));
const log = createLog(document.querySelector("[data-log-section]"));
let orders = null;
const ordersPanel = document.querySelector("[data-orders]");
if (ordersPanel) {
    orders = createOrders(ordersPanel, { send, onAttackerChange: (code) => cards.setAttacker(code) });
}

/** Adds events not seen before. Returns true if any were new, and whether one was a roll worth animating. */
function addEvents(incoming) {
    const merged = mergeEvents(events, incoming);
    const known = new Set(events.map((gameEvent) => gameEvent.sequence));
    const liveRoll = incoming.some((gameEvent) => gameEvent.type === "DiceRolled" && !known.has(gameEvent.sequence)
        && Date.now() - Date.parse(gameEvent.createdAt) < LIVE_ROLL_MAX_AGE_MS);
    const changed = merged !== events;
    events = merged;
    return { changed, liveRoll };
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

/** The events side of the page. It updates at once: the arena, the commentary strip and the log. */
function refreshEvents(animate) {
    const holdMs = arena.showBattle(events, state, animate);
    arena.showCommentary(events);
    log.render(events, state);
    if (holdMs > 0) {
        holdUntil = Date.now() + holdMs;
    }
}

/** The state side of the page: the cards, banner, players and orders. */
function renderNow(animate = true) {
    cards.patch(state, animate);
    patchBanner(state);
    patchPlayers(state, animate);
    board.dataset.turn = state.canAct ? "owner" : "other";

    if (patchGameOver(state)) {
        orders = null;
    }

    orders?.patch(state, busy, errorMessage);

    if (state.canAct && !wasPlayersTurn) {
        showToast(`Your turn. ${state.nextStep}`);
    }

    wasPlayersTurn = state.canAct;
}

/** After an order a control may have been hidden, and it took the keyboard with it, so hand the keyboard to the orders panel. */
function restoreFocus() {
    if (orders && document.activeElement === document.body) {
        orders.focus();
    }
}

/** Shows the dice before the counters they change, but never makes the counters wait more than the hold. The latest state is always what is drawn. */
function renderSoon() {
    const wait = holdUntil - Date.now();
    if (wait <= 0) {
        renderNow();
        return;
    }

    renderTimer ??= setTimeout(() => {
        renderTimer = null;
        renderNow();
    }, wait);
}

function onLiveState(incoming) {
    if (incoming.version <= state.version) {
        return;
    }

    if (state.viewerIsOwner && incoming.status === "InProgress" && incoming.currentPlayerId === humanPlayerId()) {
        // The owner's turn: the pushed state has no actions in it, so fetch the owner's own view instead of showing it.
        sync();
        return;
    }

    // The pushed state is the spectator's view, so the owner keeps their own role and has no odds in it.
    state = { ...incoming, viewerIsOwner: state.viewerIsOwner };
    renderSoon();
    afterStateChange();
}

function onLiveEvents(incoming) {
    if (hasMissedEvents(events, incoming)) {
        sync(); // a message was missed, so ask for everything after what we have
        return;
    }

    const { changed, liveRoll } = addEvents(incoming);
    if (changed) {
        refreshEvents(liveRoll);
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
                const { changed, liveRoll } = addEvents(result.newEvents);
                const stateChanged = applyOwnState(result.state);
                if (changed) {
                    refreshEvents(liveRoll);
                }

                if (stateChanged) {
                    renderSoon();
                    afterStateChange();
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

async function send(action, body) {
    if (busy) {
        return;
    }

    busy = true;
    errorMessage = "";
    orders?.patch(state, busy, errorMessage);

    try {
        const response = await fetch(`/api/games/${state.gameId}/${action}`, {
            method: "POST",
            headers: { "Content-Type": "application/json", RequestVerificationToken: requestToken },
            body: body === undefined ? undefined : JSON.stringify(body),
        });
        const result = await response.json().catch(() => ({}));

        if (response.ok) {
            const { liveRoll } = addEvents(result.newEvents);
            refreshEvents(liveRoll);
            applyOwnState(result.state);
            afterStateChange();
            showToast(action === "fast-forward" ? `Fast-forward ${result.state.isFastForward ? "on" : "off"}` : DONE_TEXT[action]);
        } else {
            errorMessage = result.error ?? "The server could not carry that out. Check your orders and try again.";
            if (response.status === 409) {
                errorMessage = "The game moved on while you were deciding. The board has been refreshed: check it and give your order again.";
                await sync();
            }

            showToast(errorMessage, "alert");
        }
    } catch {
        errorMessage = "The server could not be reached. Check your connection, then give your order again.";
        showToast(errorMessage, "alert");
    }

    busy = false;
    renderSoon();
    setTimeout(restoreFocus, Math.max(0, holdUntil - Date.now()) + 50);
}

startShare(document.querySelector("[data-share]"));
refreshEvents(false);
renderNow(false);
cards.endDeal();

if (state.status === "InProgress") {
    live = startLiveUpdates(state.gameId, {
        onEvents: onLiveEvents,
        onState: onLiveState,
        onStatus: patchLiveStatus,
        sync,
    });
}
