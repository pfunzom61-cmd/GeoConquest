// The turn banner, the live-connection line, the players list and the game-over summary: everything that says where
// the game stands. The server drew these once; this module patches their text and visibility.

import { setText } from "./dom.js";
import { countryCount, phaseLabel, resultTitle, turnTitle } from "./board-text.js";

// Written out, not just coloured, so the connection state is clear to everyone.
const LIVE_STATUS_TEXT = {
    connecting: "Live updates: connecting",
    live: "Live updates: connected",
    reconnecting: "Live updates: connection lost, reconnecting and checking for news meanwhile",
    polling: "Live updates: unavailable, checking for news every few seconds",
};

export function patchLiveStatus(status) {
    const node = document.querySelector('[data-bind="live-status"]');
    node.dataset.live = status;
    node.textContent = LIVE_STATUS_TEXT[status];
}

export function patchBanner(state) {
    const banner = document.querySelector("[data-banner]");
    const current = state.players.find((player) => player.id === state.currentPlayerId);

    setText(banner, "turn-title", turnTitle(state));
    setText(banner, "turn-initials", current.initials);
    banner.querySelector("[data-bind-badge]").dataset.color = current.colorKey;
    setText(banner, "next-step", state.nextStep);
    setText(banner, "round", String(state.round));
    setText(banner, "phase", phaseLabel(state.phase));
    banner.querySelector('[data-bind="fast-forward-note"]').hidden = !state.isFastForward;
}

/** Players still in the game stay in the main list; eliminated ones move under the collapsed "Eliminated" line. */
export function patchPlayers(state) {
    const active = document.querySelector("[data-players-active]");
    const out = document.querySelector("[data-players-eliminated]");

    for (const item of document.querySelectorAll("[data-player]")) {
        const player = state.players.find((candidate) => candidate.id === item.dataset.player);

        setText(item, "player-count", countryCount(player.countryCount));
        item.querySelector('[data-bind="player-now"]').hidden = state.status !== "InProgress" || player.id !== state.currentPlayerId;
        item.dataset.eliminated = String(player.isEliminated);

        const wantedList = player.isEliminated ? out : active;
        if (item.parentElement !== wantedList) {
            wantedList.append(item);
        }
    }

    const eliminated = state.players.filter((player) => player.isEliminated).length;
    setText(document, "eliminated-count", String(eliminated));
    document.querySelector("[data-players-out]").hidden = eliminated === 0;
}

/** When the game ends the orders go and the summary appears. The server also leaves the orders out of a finished game's page. */
export function patchGameOver(state) {
    const summary = document.querySelector("[data-gameover]");
    if (state.status === "InProgress") {
        summary.hidden = true;
        return false;
    }

    document.querySelector("[data-orders]")?.remove();

    const human = state.players.find((player) => player.isHuman);
    setText(summary, "result-title", resultTitle(state.status));
    setText(summary, "next-step", state.nextStep);
    setText(summary, "round", String(state.round));
    setText(summary, "human-countries", String(human.countryCount));
    summary.hidden = false;
    return true;
}
