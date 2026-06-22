document.addEventListener("DOMContentLoaded", () => {
    const detail = document.querySelector("[data-product-detail]");
    if (!detail) return;

    const basePrice = Number.parseFloat(detail.dataset.basePrice || "0");
    const mainPrice = detail.querySelector("[data-product-total-price]");
    const summaryPrice = detail.querySelector("[data-selection-total]");
    const selectionText = detail.querySelector("[data-selection-text]");
    const formatter = new Intl.NumberFormat("vi-VN");
    const optionInputs = Array.from(detail.querySelectorAll("[data-option-group] input"));

    optionInputs.forEach(input => {
        input.checked = false;
    });

    function getPriceRange() {
        let minimumPrice = basePrice;
        let maximumPrice = basePrice;

        detail.querySelectorAll("[data-option-group]").forEach(group => {
            const prices = Array.from(group.querySelectorAll("input"))
                .map(input => Number.parseFloat(input.dataset.additionalPrice || "0"));

            if (!prices.length) return;

            minimumPrice += Math.min(...prices);
            maximumPrice += Math.max(...prices);
        });

        return minimumPrice === maximumPrice
            ? `${formatter.format(minimumPrice)}đ`
            : `${formatter.format(minimumPrice)}đ - ${formatter.format(maximumPrice)}đ`;
    }

    function updateSelection() {
        const selected = Array.from(detail.querySelectorAll("[data-option-group] input:checked"));
        const additionalPrice = selected.reduce(
            (total, input) => total + Number.parseFloat(input.dataset.additionalPrice || "0"),
            0);
        const total = basePrice + additionalPrice;
        const formattedPrice = selected.length
            ? `${formatter.format(total)}đ`
            : getPriceRange();

        if (mainPrice) mainPrice.textContent = formattedPrice;
        if (summaryPrice) summaryPrice.textContent = formattedPrice;
        if (selectionText) {
            selectionText.textContent = selected.length
                ? selected.map(input => `${input.dataset.optionName}: ${input.dataset.valueName}`).join(" · ")
                : "Chưa chọn đầy đủ";
        }
    }

    detail.querySelectorAll(".product-option-button").forEach(button => {
        const input = button.querySelector("input[type='radio']");
        if (!input) return;

        button.addEventListener("pointerdown", () => {
            button.dataset.wasChecked = input.checked ? "true" : "false";
        });

        button.addEventListener("click", event => {
            if (button.dataset.wasChecked !== "true") return;

            event.preventDefault();
            input.checked = false;
            button.dataset.wasChecked = "false";
            updateSelection();
        });

        input.addEventListener("keydown", event => {
            if (event.key !== " " || !input.checked) return;

            event.preventDefault();
            input.checked = false;
            updateSelection();
        });

        input.addEventListener("change", updateSelection);
    });

    updateSelection();
});
