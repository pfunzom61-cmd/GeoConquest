// The owner's orders. The server drew the forms once; this module shows the ones that fit the phase, fills their lists,
// and turns clicks into intents for the server. A button that cannot be used says why, in words next to it. The UI only
// hides and disables: the server still rejects anything the rules do not allow.

import { el } from "./dom.js";
import { askConfirmation } from "./confirm.js";
import { formatChance, plural, playerLabel } from "./board-text.js";

const ENDPOINT = { endTurn: "end-turn" };

/** Replaces a select's options only when the list changed, so the player's choice and focus survive a refresh. */
function fillSelect(select, items) {
    const signature = items.map((item) => `${item.value}|${item.label}`).join(";");
    if (select.dataset.signature === signature) {
        return;
    }

    const chosen = select.value;
    select.replaceChildren(...items.map((item) => el("option", { value: item.value, textContent: item.label })));
    select.dataset.signature = signature;
    if (items.some((item) => item.value === chosen)) {
        select.value = chosen;
    }
}

export function createOrders(panel, { send, onAttackerChange }) {
    const find = (selector) => panel.querySelector(selector);
    const inputs = [...panel.querySelectorAll("[data-allocate-input]")];
    const [transferFrom, transferTo, transferCount] = [find("[data-transfer-from]"), find("[data-transfer-to]"), find("[data-transfer-count]")];
    const [attackFrom, attackTarget] = [find("[data-attack-from]"), find("[data-attack-target]")];
    let state = null;
    let busy = false;

    /** Enables or disables a button, and says in words why it cannot be used. */
    function setReason(button, reason) {
        const note = panel.querySelector(`#${button.getAttribute("aria-describedby")}`);
        note.textContent = reason;
        note.hidden = reason === "";
        button.disabled = busy || reason !== "";
    }

    const submitButton = (action) => find(`[data-action="${action}"]`);
    const humanId = () => state.players.find((player) => player.isHuman).id;
    const owned = () => state.territories.filter((territory) => territory.ownerPlayerId === humanId());
    const territory = (code) => state.territories.find((candidate) => candidate.countryCode === code);

    function patchAllocate() {
        const mine = owned();
        const pending = state.pendingReinforcements;
        let placed = 0;

        for (const input of inputs) {
            const own = mine.find((candidate) => candidate.countryCode === input.dataset.allocateInput);
            input.closest("[data-allocate-row]").hidden = !own;
            input.max = String(pending);
            if (own) {
                input.closest("[data-allocate-row]").querySelector('[data-bind="allocate-current"]').textContent = `(has ${plural(own.soldiers, "soldier")})`;
            }

            if (!state.availableActions.includes("allocate")) {
                input.value = "0";
            }

            placed += Number(input.value) || 0;
        }

        panel.querySelector('[data-bind="allocate-total"]').textContent = `${placed} of ${pending} placed`;
        const reason = placed < pending
            ? `Place ${plural(pending - placed, "more soldier")} to finish.`
            : placed > pending ? `Remove ${plural(placed - pending, "soldier")}: you only have ${pending} to place.` : "";
        setReason(submitButton("allocate"), reason);
    }

    function patchTransfer() {
        const items = owned().map((own) => ({ value: own.countryCode, label: `${own.countryName} (${own.soldiers})` }));
        fillSelect(transferFrom, items);
        fillSelect(transferTo, items);

        const source = territory(transferFrom.value);
        const movable = source ? source.soldiers - 1 : 0;
        transferCount.max = String(Math.max(movable, 1));

        let reason = "";
        if (items.length < 2) {
            reason = "You need two countries to move soldiers between.";
        } else if (transferFrom.value === transferTo.value) {
            reason = "Choose two different countries.";
        } else if (movable < 1) {
            reason = `${source.countryName} has only 1 soldier and must keep it.`;
        } else if (!(Number(transferCount.value) >= 1) || Number(transferCount.value) > movable) {
            reason = `Move between 1 and ${movable}: ${source.countryName} must keep 1 soldier.`;
        }

        setReason(submitButton("transfer"), reason);
    }

    function patchAttack() {
        const attackers = owned().filter((own) => own.soldiers >= 2);
        const targets = state.territories.filter((candidate) => candidate.ownerPlayerId !== humanId());
        fillSelect(attackFrom, attackers.map((own) => ({ value: own.countryCode, label: `${own.countryName} (${own.soldiers})` })));
        fillSelect(attackTarget, targets.map((target) => ({
            value: target.countryCode,
            label: `${target.countryName} (${target.soldiers}), ${playerLabel(state.players.find((player) => player.id === target.ownerPlayerId), true)}`,
        })));

        const odds = state.attackOdds.find((entry) =>
            entry.attackerCountryCode === attackFrom.value && entry.defenderCountryCode === attackTarget.value);
        panel.querySelector('[data-bind="attack-odds"]').textContent = odds ? `Estimated win chance: ${formatChance(odds.winChance)}` : "";
        setReason(submitButton("attack"), attackers.length === 0 ? "Needs 2 soldiers to attack: none of your countries has 2." : "");
        onAttackerChange(attackFrom.value);
    }

    function patchBattle() {
        const battle = state.battle;
        panel.querySelector('[data-bind="battle-summary"]').textContent = battle
            ? `${battle.attackerName} is attacking ${battle.defenderName}. ${plural(battle.roundsFought, "round")} fought.`
            : "";
        const odds = state.attackOdds[0];
        panel.querySelector('[data-bind="battle-odds"]').textContent = battle && odds ? `Estimated win chance now: ${formatChance(odds.winChance)}` : "";
    }

    function patchPace() {
        const toggle = find("[data-fast-forward]");
        toggle.textContent = state.isFastForward ? "Turn fast-forward off" : "Fast-forward NPC turns";
        toggle.setAttribute("aria-pressed", String(state.isFastForward));
        toggle.disabled = busy;
        panel.querySelector('[data-bind="fast-forward-hint"]').textContent =
            "Speeds up the NPC turns after you end yours. It switches itself off when your turn comes round again.";
    }

    function patch(newState, isBusy, errorMessage) {
        state = newState;
        busy = isBusy;

        const error = find("[data-error]");
        error.textContent = errorMessage;
        error.hidden = errorMessage === "";

        const waiting = find("[data-orders-waiting]");
        const current = state.players.find((player) => player.id === state.currentPlayerId);
        waiting.textContent = `Waiting for ${playerLabel(current, true)}. Your orders appear here when it is your turn.`;
        waiting.hidden = state.canAct;

        for (const button of panel.querySelectorAll("[data-action]")) {
            button.hidden = !state.availableActions.includes(button.dataset.action);
            button.disabled = busy;
        }

        for (const group of panel.querySelectorAll("[data-group]")) {
            group.hidden = !group.querySelector("[data-action]:not([hidden])");
        }

        patchAllocate();
        patchTransfer();
        patchAttack();
        patchBattle();
        patchPace();
    }

    for (const [group, buildBody] of [
        ["allocate", () => ({ allocations: Object.fromEntries(inputs.filter((input) => Number(input.value) > 0).map((input) => [input.dataset.allocateInput, Number(input.value)])) })],
        ["transfer", () => ({ fromCountryCode: transferFrom.value, toCountryCode: transferTo.value, soldiers: Number(transferCount.value) })],
        ["attack", () => ({ attackerCountryCode: attackFrom.value, defenderCountryCode: attackTarget.value })],
    ]) {
        find(`[data-group="${group}"]`).addEventListener("submit", (event) => {
            event.preventDefault();
            send(group, buildBody());
        });
    }

    // Typing in any field re-checks the form, so a mistake is explained next to the button before it is pressed.
    panel.addEventListener("input", () => state && patch(state, busy, find("[data-error]").textContent));
    panel.addEventListener("change", () => state && patch(state, busy, find("[data-error]").textContent));

    for (const button of panel.querySelectorAll('button[type="button"][data-action]')) {
        button.addEventListener("click", async () => {
            if (button.dataset.confirmTitle && !(await askConfirmation({
                title: button.dataset.confirmTitle, body: button.dataset.confirmBody, confirmLabel: button.dataset.confirmLabel,
            }))) {
                return;
            }

            send(ENDPOINT[button.dataset.action] ?? button.dataset.action);
        });
    }

    find("[data-fast-forward]").addEventListener("click", () => send("fast-forward", { enabled: !state.isFastForward }));

    return { patch, focus: () => panel.focus() };
}
