// Filters the New campaign country list as the player types. Without JavaScript the full list is still usable.
const filterInput = document.getElementById("country-filter");
const items = [...document.querySelectorAll("#country-picker [data-name]")];
const status = document.getElementById("country-filter-status");

filterInput.addEventListener("input", () => {
    const term = filterInput.value.trim().toLowerCase();
    let visible = 0;

    for (const item of items) {
        const matches = item.dataset.name.includes(term);
        item.hidden = !matches;
        if (matches) {
            visible++;
        }
    }

    if (!term) {
        status.textContent = "";
    } else {
        status.textContent = visible === 0
            ? "No country matches. Clear the search to see them all."
            : `${visible} ${visible === 1 ? "country matches" : "countries match"}.`;
    }
});
