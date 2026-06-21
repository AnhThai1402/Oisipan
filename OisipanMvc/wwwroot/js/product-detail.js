document.addEventListener("DOMContentLoaded", () => {
    const detail = document.querySelector("[data-product-detail]");
    if (!detail) return;

    const basePrice = Number.parseFloat(detail.dataset.basePrice || "0");
    const mainPrice = detail.querySelector("[data-product-total-price]");
    const summaryPrice = detail.querySelector("[data-selection-total]");
    const selectionText = detail.querySelector("[data-selection-text]");
    const formatter = new Intl.NumberFormat("vi-VN");

    function updateSelection() {
        const selected = Array.from(detail.querySelectorAll("[data-option-group] input:checked"));
        const additionalPrice = selected.reduce(
            (total, input) => total + Number.parseFloat(input.dataset.additionalPrice || "0"),
            0);
        const total = basePrice + additionalPrice;
        const formattedPrice = `${formatter.format(total)}đ`;

        if (mainPrice) mainPrice.textContent = formattedPrice;
        if (summaryPrice) summaryPrice.textContent = formattedPrice;
        if (selectionText) {
            selectionText.textContent = selected.length
                ? selected.map(input => `${input.dataset.optionName}: ${input.dataset.valueName}`).join(" · ")
                : "Chưa chọn đầy đủ";
        }
    }

    detail.querySelectorAll("[data-option-group] input").forEach(input => {
        input.addEventListener("change", updateSelection);
    });

    updateSelection();
});
