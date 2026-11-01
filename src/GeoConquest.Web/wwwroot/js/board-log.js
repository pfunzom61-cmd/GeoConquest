// The event log: newest first, with filter chips. It starts on "All", so every NPC decision is visible by default.

import { el } from "./dom.js";
import { describeEvent, eventKind } from "./board-text.js";

const FILTER_KINDS = {
    all: null,
    battles: "battle",
    npc: "npc",
    commentary: "commentary",
};

export function createLog(section) {
    const list = section.querySelector("[data-log]");
    const empty = section.querySelector('[data-bind="log-empty"]');
    const chips = [...section.querySelectorAll("[data-filter]")];
    let filter = "all";
    let lastEvents = [];
    let lastState = null;

    function render(events, state) {
        lastEvents = events;
        lastState = state;

        const wanted = FILTER_KINDS[filter];
        const shown = [...events].reverse().filter((gameEvent) => wanted === null || eventKind(gameEvent.type) === wanted);

        list.replaceChildren(...shown.map((gameEvent) => el("li",
            { className: "gc-log__entry", "data-kind": eventKind(gameEvent.type) },
            el("span", { className: "gc-log__seq", textContent: `#${gameEvent.sequence}` }),
            ` ${describeEvent(gameEvent, state)}`)));
        empty.hidden = shown.length > 0;
    }

    for (const chip of chips) {
        chip.addEventListener("click", () => {
            filter = chip.dataset.filter;
            chips.forEach((other) => other.setAttribute("aria-pressed", String(other === chip)));
            render(lastEvents, lastState);
        });
    }

    return { render };
}
