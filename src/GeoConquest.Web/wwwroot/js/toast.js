// Toasts: a short message for every result. Results go to the polite status region, problems to the alert region.

const SHOW_MS = { status: 4000, alert: 8000 };

/** @param {"status" | "alert"} kind */
export function showToast(message, kind = "status") {
    const stack = document.querySelector(`[data-toasts="${kind}"]`);
    if (!stack) {
        return;
    }

    const toast = document.createElement("p");
    toast.className = kind === "alert" ? "gc-toast gc-toast--error" : "gc-toast";
    toast.textContent = kind === "alert" ? `Problem: ${message}` : message;
    stack.append(toast);

    setTimeout(() => toast.remove(), SHOW_MS[kind]);
}
