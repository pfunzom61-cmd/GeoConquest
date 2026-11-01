// The "Copy game link" button: copies the address of this game and confirms with a toast.

import { showToast } from "./toast.js";

export function startShare(button) {
    button.addEventListener("click", async () => {
        const link = `${location.origin}${location.pathname}`;
        try {
            await navigator.clipboard.writeText(link);
            showToast("Game link copied");
        } catch {
            showToast("The link could not be copied. Copy the address from your browser's address bar instead.", "alert");
        }
    });
}
