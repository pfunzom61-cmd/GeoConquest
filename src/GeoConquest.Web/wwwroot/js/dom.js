// Tiny DOM helpers shared by the board modules. Text only ever goes in through textContent.

/** Builds an element. Properties are set directly, "aria-" and "data-" names as attributes. */
export function el(tag, props = {}, ...children) {
    const node = document.createElement(tag);
    for (const [name, value] of Object.entries(props)) {
        if (name.startsWith("aria-") || name.startsWith("data-")) {
            node.setAttribute(name, value);
        } else {
            node[name] = value;
        }
    }

    node.append(...children);
    return node;
}

/** Sets the text of the element marked data-bind="name" inside root, only if it changed. Returns that element. */
export function setText(root, name, text) {
    const node = root.querySelector(`[data-bind="${name}"]`);
    if (node && node.textContent !== text) {
        node.textContent = text;
    }

    return node;
}
