// Draws the game board and posts the owner's intents. The server decides everything: this script never rolls dice or
// checks rules, it only shows the state and events the server returns. All text goes in through textContent.

const MAX_EVENTS = 100;

const pageData = JSON.parse(document.getElementById("board-data").textContent);
const requestToken = document.querySelector('meta[name="request-verification-token"]')?.content ?? "";
const root = document.getElementById("board");

let state = pageData.state;
let events = pageData.recentEvents;
let busy = false;
let errorMessage = "";

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
    const humanId = state.players.find((player) => player.isHuman).id;
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
            state = result.state;
            events = [...events, ...result.newEvents].slice(-MAX_EVENTS);
        } else {
            errorMessage = result.error ?? "Something went wrong. Please try again.";
            if (response.status === 409) {
                await refreshState();
            }
        }
    } catch {
        errorMessage = "Could not reach the server. Check your connection and try again.";
    }

    busy = false;
    render();
}

async function refreshState() {
    const response = await fetch(`/api/games/${state.gameId}`);
    if (response.ok) {
        state = await response.json();
    }
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
    return el("section", { "aria-label": "Turn" },
        el("h2", { textContent: `Round ${state.round}: ${state.status === "InProgress" ? `${state.currentPlayerName}'s turn` : "Game over"}` }),
        el("p", { role: "status", textContent: state.nextStep }),
        el("p", { textContent: `Phase: ${state.phase}` }));
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

    panel.append(buildReinforceForm(), buildTransferForm(), buildAttackForm(), buildBattleControls(), buildTurnControls());
    return panel;
}

function buildEventLog() {
    const entries = [...events].reverse().map((gameEvent) =>
        el("li", { textContent: `#${gameEvent.sequence} ${gameEvent.message}` }));

    return el("section", { "aria-label": "Event log" },
        el("h2", { textContent: "Event log (latest first)" }),
        el("ul", {}, ...entries));
}

function render() {
    // Keep the keyboard where it was: a re-render replaces the controls, so remember which one had focus.
    const focusedId = document.activeElement?.id;
    root.replaceChildren(buildBanner(), buildTerritories(), buildActionPanel(), buildEventLog());
    if (focusedId) {
        document.getElementById(focusedId)?.focus();
    }
}

render();
