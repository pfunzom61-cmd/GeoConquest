// Keeps the board in step with the server while other people (the NPCs) are playing. It prefers a SignalR connection
// and falls back to asking the server for the latest state every few seconds, so the board still moves if
// WebSockets (and the long-polling fallback) are blocked or the connection drops. The server never receives anything
// from this connection: it only listens.

const POLL_INTERVAL_MS = 5000;
const RECONNECT_ATTEMPT_INTERVAL_MS = 15000;
const AUTOMATIC_RECONNECT_DELAYS_MS = [0, 2000, 5000, 10000, 20000, 30000];

/**
 * @param {string} gameId
 * @param {{
 *   onEvents: (events: object[]) => void,
 *   onState: (state: object) => void,
 *   onStatus: (status: "connecting" | "live" | "reconnecting" | "polling") => void,
 *   sync: () => Promise<void>,
 * }} handlers
 * @returns {{ stop: () => void }}
 */
export function startLiveUpdates(gameId, { onEvents, onState, onStatus, sync }) {
    let connection = null;
    let pollTimer = null;
    let retryTimer = null;
    let stopped = false;

    function startPolling() {
        pollTimer ??= setInterval(() => sync().catch(() => {}), POLL_INTERVAL_MS);
    }

    function stopPolling() {
        clearInterval(pollTimer);
        pollTimer = null;
    }

    function fallBackToPolling() {
        if (stopped) {
            return;
        }

        onStatus("polling");
        startPolling();
        clearTimeout(retryTimer);
        retryTimer = setTimeout(connect, RECONNECT_ATTEMPT_INTERVAL_MS);
    }

    async function connect() {
        if (stopped) {
            return;
        }

        // The client script is a plain script tag; if it failed to load, polling still keeps the board current.
        if (typeof signalR === "undefined") {
            fallBackToPolling();
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl(`/hubs/game?gameId=${encodeURIComponent(gameId)}`)
            .withAutomaticReconnect(AUTOMATIC_RECONNECT_DELAYS_MS)
            .build();

        connection.on("EventsAppended", onEvents);
        connection.on("StateUpdated", onState);
        connection.onreconnecting(() => {
            onStatus("reconnecting");
            startPolling();
        });
        connection.onreconnected(async () => {
            stopPolling();
            onStatus("live");
            await sync().catch(() => {}); // whatever happened while disconnected
        });
        connection.onclose(fallBackToPolling);

        try {
            await connection.start();
            stopPolling();
            onStatus("live");
            await sync().catch(() => {}); // anything that happened between loading the page and connecting
        } catch {
            fallBackToPolling();
        }
    }

    onStatus("connecting");
    connect();

    return {
        stop() {
            stopped = true;
            stopPolling();
            clearTimeout(retryTimer);
            connection?.stop();
        },
    };
}
