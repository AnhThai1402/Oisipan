(function () {
    "use strict";

    const formatter = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 });

    function rawValue(value) {
        return String(value || "").replace(/[^\d]/g, "");
    }

    function formatInput(input) {
        const raw = rawValue(input.value);
        input.value = raw ? formatter.format(Number(raw)) : "";
    }

    function initialize(root) {
        root.querySelectorAll(".money-input").forEach(input => {
            if (input.dataset.moneyReady === "true") return;
            input.dataset.moneyReady = "true";
            input.setAttribute("inputmode", "numeric");
            input.addEventListener("input", () => formatInput(input));
            input.addEventListener("focus", () => input.select());
            formatInput(input);
        });
    }

    document.addEventListener("DOMContentLoaded", () => initialize(document));

    document.addEventListener("submit", event => {
        event.target.querySelectorAll?.(".money-input").forEach(input => {
            input.value = rawValue(input.value) || "0";
        });
    }, true);

    window.initializeMoneyInputs = initialize;
    window.getMoneyInputValue = input => Number(rawValue(input?.value) || "0");
})();
