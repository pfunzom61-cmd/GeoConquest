// A loading state for forms that reload the page: the submit button says what is happening and cannot be pressed twice.

for (const form of document.querySelectorAll("[data-busy-form]")) {
    const button = form.querySelector("button[type=submit]");
    const idleText = button.textContent;

    form.addEventListener("submit", () => {
        form.setAttribute("aria-busy", "true");
        button.textContent = `${button.dataset.busyText ?? "Working"}…`;
        button.disabled = true;
    });

    // Coming back with the browser's back button restores the page from memory, so put the button back.
    window.addEventListener("pageshow", () => {
        form.removeAttribute("aria-busy");
        button.textContent = idleText;
        button.disabled = false;
    });
}
