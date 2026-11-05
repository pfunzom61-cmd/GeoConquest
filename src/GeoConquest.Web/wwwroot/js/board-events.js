// The board's copy of the event log. A dice event's commentary has a reserved place right after it but is written a
// moment later, so events can arrive out of order and a place in the log can stay empty for a while.

const MAX_EVENTS = 100;

// A round's commentary takes a few seconds at most, so a place still empty after this long will stay that way (the
// server restarted before writing it). A viewer's clock being off only changes how long polling keeps looking.
const EMPTY_SLOT_PATIENCE_MS = 60000;

export function lastSequence(events) {
    return events.length > 0 ? events[events.length - 1].sequence : 0;
}

/** Returns the log with the unseen events added in their proper places, or the same array if nothing was new. */
export function mergeEvents(events, incoming) {
    const known = new Set(events.map((gameEvent) => gameEvent.sequence));
    const fresh = incoming.filter((gameEvent) => !known.has(gameEvent.sequence));
    if (fresh.length === 0) {
        return events;
    }

    return [...events, ...fresh].sort((first, second) => first.sequence - second.sequence).slice(-MAX_EVENTS);
}

/** True if events newer than the log start beyond the next expected number, allowing for an empty place after dice. */
export function hasMissedEvents(events, incoming) {
    const last = lastSequence(events);
    const newer = incoming.map((gameEvent) => gameEvent.sequence).filter((sequence) => sequence > last);
    if (newer.length === 0) {
        return false;
    }

    const lastIsDice = events[events.length - 1]?.type === "DiceRolled";
    return Math.min(...newer) > last + (lastIsDice ? 2 : 1);
}

/**
 * The number to ask the server for events after. It looks back from the oldest place still expected to fill, so a
 * viewer who is polling gets commentary that landed behind newer events. An empty place stops counting once it is too
 * old. If nothing has happened since those dice they are the last event, which is where the search starts anyway.
 */
export function syncCursor(events, nowMs) {
    const known = new Set(events.map((gameEvent) => gameEvent.sequence));
    const awaited = events.find((gameEvent) =>
        gameEvent.type === "DiceRolled"
        && !known.has(gameEvent.sequence + 1)
        && nowMs - Date.parse(gameEvent.createdAt) < EMPTY_SLOT_PATIENCE_MS);

    return awaited ? awaited.sequence : lastSequence(events);
}
