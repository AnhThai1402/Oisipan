document.addEventListener("DOMContentLoaded", () => {
    const list = document.querySelector("#product-variant-list");
    const template = document.querySelector("#product-variant-template");
    const addButton = document.querySelector("#add-product-variant");
    const totalQuantity = document.querySelector("#Quantity");

    if (!list || !template || !addButton || !totalQuantity) {
        return;
    }

    function setName(input, index, property) {
        input.name = `ProductVariants[${index}].${property}`;
        input.id = `ProductVariants_${index}__${property}`;
    }

    function reindex() {
        list.querySelectorAll(".product-variant-row").forEach((row, index) => {
            const idInput = row.querySelector(".variant-id");
            if (idInput) {
                setName(idInput, index, "ProductVariantId");
            }

            setName(row.querySelector(".variant-size"), index, "Size");
            setName(row.querySelector(".variant-filling"), index, "Filling");
            setName(row.querySelector(".variant-price"), index, "AdditionalPrice");
            setName(row.querySelector(".variant-quantity"), index, "Quantity");
            setName(row.querySelector(".variant-active-fallback"), index, "IsActive");
            setName(row.querySelector(".variant-active"), index, "IsActive");
        });

        updateTotalQuantity();
    }

    function updateTotalQuantity() {
        const total = Array.from(list.querySelectorAll(".variant-quantity"))
            .reduce((sum, input) => sum + Math.max(0, Number.parseInt(input.value || "0", 10)), 0);
        totalQuantity.value = total;
    }

    addButton.addEventListener("click", () => {
        const row = template.content.firstElementChild.cloneNode(true);
        list.appendChild(row);
        reindex();
        row.querySelector(".variant-size").focus();
    });

    list.addEventListener("click", (event) => {
        const removeButton = event.target.closest(".remove-product-variant");
        if (!removeButton) {
            return;
        }

        removeButton.closest(".product-variant-row").remove();
        reindex();
    });

    list.addEventListener("input", (event) => {
        if (event.target.matches(".variant-quantity")) {
            updateTotalQuantity();
        }
    });

    reindex();
});
