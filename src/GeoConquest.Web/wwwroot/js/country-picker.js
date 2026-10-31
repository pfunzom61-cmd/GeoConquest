// Filters the New Campaign country list as the player types. Without JavaScript the full list is still usable.
const filterInput = document.getElementById("country-filter");
const rows = [...document.querySelectorAll("#country-picker tbody tr")];
const status = document.getElementById("country-filter-status");

filterInput.addEventListener("input", () => {
    const term = filterInput.value.trim().toLowerCase();
    let visible = 0;

    for (const row of rows) {
        const matches = row.dataset.name.includes(term);
        row.hidden = !matches;
        if (matches) {
            visible++;
        }
    }

    status.textContent = term ? `${visible} countries match.` : "";
});
