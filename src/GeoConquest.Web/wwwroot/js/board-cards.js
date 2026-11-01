// The ten dossier cards. The server draws them once; this module only patches the nodes already on the page through
// their data- attributes (soldiers, owner), starts the counter, flash and stamp animations, and sorts and highlights them.

import { el, setText } from "./dom.js";
import { formatChance, playerLabel } from "./board-text.js";
import { prefersReducedMotion, replay } from "./motion.js";

const SORTERS = {
    board: () => 0,
    "soldiers-desc": (a, b) => b.soldiers - a.soldiers,
    "soldiers-asc": (a, b) => a.soldiers - b.soldiers,
    owner: (a, b) => a.ownerName.localeCompare(b.ownerName),
    country: (a, b) => a.countryName.localeCompare(b.countryName),
};

/** Picture-less fallback: a flag that fails to load is hidden, which leaves the country code that sits behind it. */
function hideBrokenFlags(grid) {
    for (const image of grid.querySelectorAll(".gc-flag__image")) {
        image.hidden = image.complete && image.naturalWidth === 0; // already failed before this script ran
    }

    grid.addEventListener("error", (event) => {
        if (event.target.matches?.(".gc-flag__image")) {
            event.target.hidden = true;
        }
    }, true);
}

export function createCards(section, viewerIsOwner) {
    const grid = section.querySelector("[data-card-grid]");
    const sortSelect = section.querySelector("[data-card-sort]");
    const filterSelect = section.querySelector("[data-card-filter]");
    const cards = [...grid.querySelectorAll("[data-card]")];
    let lastState = null;
    let attackerCode = "";

    hideBrokenFlags(grid);

    function ownerOf(state, territory) {
        return state.players.find((player) => player.id === territory.ownerPlayerId);
    }

    function patchCard(card, state, animate) {
        const territory = state.territories.find((candidate) => candidate.countryCode === card.dataset.country);
        const owner = ownerOf(state, territory);
        const stamp = card.querySelector("[data-stamp]");
        const animateNow = animate && !prefersReducedMotion();

        if (Number(card.dataset.soldiers) !== territory.soldiers) {
            card.dataset.soldiers = String(territory.soldiers);
            const counter = setText(card, "soldiers", String(territory.soldiers));
            if (animateNow && counter) {
                replay(counter, "gc-counter--tick");
            }
        }

        if (card.dataset.owner !== owner.id) {
            card.dataset.owner = owner.id;
            card.dataset.color = owner.colorKey;
            setText(card, "owner-initials", owner.initials);
            setText(card, "owner-label", playerLabel(owner, state.viewerIsOwner));
            if (animateNow) {
                replay(card, "gc-card--flash");
            }
        }

        // A country held by anyone but the player who started with it carries the stamp; taking it back removes it.
        const conquered = owner.homeCountryCode !== territory.countryCode;
        if (stamp.hidden === conquered) {
            stamp.hidden = !conquered;
            if (conquered && animateNow) {
                replay(stamp, "gc-stamp--slam");
            }
        }

        card.dataset.yours = String(state.viewerIsOwner && owner.isHuman);
    }

    /** Countries the owner could attack now are framed and say so in words; with a chosen attacker, the odds too. */
    function markTargets(state) {
        const humanId = state.players.find((player) => player.isHuman).id;
        const choosing = state.viewerIsOwner && state.canAct && state.phase === "Action";

        for (const card of cards) {
            const isTarget = choosing && card.dataset.owner !== humanId;
            card.dataset.target = String(isTarget);

            const odds = state.attackOdds.find((entry) =>
                entry.attackerCountryCode === attackerCode && entry.defenderCountryCode === card.dataset.country);
            const note = card.querySelector('[data-bind="card-note"]');
            note.hidden = !isTarget;
            note.textContent = odds ? `Can be attacked. Win chance ${formatChance(odds.winChance)}` : "Can be attacked";
        }
    }

    function applyHighlight(state) {
        const mode = filterSelect.value;
        const humanId = state.players.find((player) => player.isHuman).id;

        for (const card of cards) {
            const matches = mode === "all"
                || (mode === "yours" && card.dataset.owner === humanId)
                || (mode === "targets" && card.dataset.target === "true")
                || mode === `player:${card.dataset.owner}`;
            card.dataset.dim = String(!matches);
        }
    }

    function applySort(state) {
        const rows = cards.map((card) => {
            const territory = state.territories.find((candidate) => candidate.countryCode === card.dataset.country);
            return { card, soldiers: territory.soldiers, ownerName: territory.ownerName, countryName: territory.countryName };
        });

        // Array.sort is stable, so equal rows keep the board order they were drawn in.
        rows.sort(SORTERS[sortSelect.value]);

        // Moving a card restarts any animation running on it, so the cards only move when the order really changes.
        const wanted = rows.map((row) => row.card);
        if (wanted.some((card, index) => card !== grid.children[index])) {
            grid.append(...wanted);
        }
    }

    function fillFilterOptions(state) {
        const options = [["all", "All countries"]];
        if (viewerIsOwner) {
            options.push(["yours", "Your countries"], ["targets", "Countries you can attack"]);
        }

        options.push(...state.players.map((player) => [`player:${player.id}`, `Held by ${playerLabel(player, viewerIsOwner)}`]));
        filterSelect.replaceChildren(...options.map(([value, text]) => el("option", { value, textContent: text })));
    }

    sortSelect.addEventListener("change", () => lastState && applySort(lastState));
    filterSelect.addEventListener("change", () => lastState && applyHighlight(lastState));

    return {
        /** @param {boolean} animate false for the first draw and for changes that happened while the player was away */
        patch(state, animate) {
            if (!lastState) {
                fillFilterOptions(state);
            }

            lastState = state;
            cards.forEach((card) => patchCard(card, state, animate));
            markTargets(state);
            applyHighlight(state);
            applySort(state);
        },

        /** The attacking country chosen in the orders, so each target can show its own win chance. */
        setAttacker(countryCode) {
            attackerCode = countryCode;
            if (lastState) {
                markTargets(lastState);
            }
        },

        /** Lets the entrance animation run once, then removes it so sorting never replays it. */
        endDeal() {
            setTimeout(() => section.classList.remove("gc-map--dealing"), 1200);
        },
    };
}
