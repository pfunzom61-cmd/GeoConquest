// A flag that fails to load is hidden, which leaves the country code that sits behind it. Runs on every page.

for (const image of document.querySelectorAll(".gc-flag__image")) {
    image.hidden = image.complete && image.naturalWidth === 0; // already failed before this script ran
}

// Image errors do not bubble, so listen in the capture phase.
document.addEventListener("error", (event) => {
    if (event.target.matches?.(".gc-flag__image")) {
        event.target.hidden = true;
    }
}, true);
