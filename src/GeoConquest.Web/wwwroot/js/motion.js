// Small helpers for the board's motion. Every animation is a CSS class, so the stylesheet decides what it looks like and
// the reduced-motion rule there turns each one into an instant change. This file only starts them.

const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

export function prefersReducedMotion() {
    return reducedMotion.matches;
}

/** Plays a CSS animation class again from the start, and removes the class when it ends. */
export function replay(element, className) {
    element.classList.remove(className);
    void element.offsetWidth; // let the browser see the class removed, so adding it restarts the animation
    element.classList.add(className);
    element.addEventListener("animationend", () => element.classList.remove(className), { once: true });
}
