// Asks "are you sure?" in the page's one dialog. Used only for things that cannot be undone.

/**
 * @param {{ title: string, body: string, confirmLabel: string }} words
 * @returns {Promise<boolean>} true if the player confirmed
 */
export function askConfirmation({ title, body, confirmLabel }) {
    const dialog = document.querySelector("[data-confirm-dialog]");
    if (!dialog || typeof dialog.showModal !== "function") {
        return Promise.resolve(true); // no dialog support: the server still validates, so do not block the player
    }

    dialog.querySelector("[data-confirm-title]").textContent = title;
    dialog.querySelector("[data-confirm-body]").textContent = body;
    dialog.querySelector("[data-confirm-accept]").textContent = confirmLabel;

    return new Promise((resolve) => {
        dialog.addEventListener("close", () => resolve(dialog.returnValue === "confirm"), { once: true });
        dialog.returnValue = "cancel";
        dialog.showModal();
    });
}
