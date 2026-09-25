(function () {
    const root = document.getElementById("variant-form-root");
    if (!root) {
        return;
    }

    const productId = root.dataset.productId;
    const variantId = root.dataset.variantId;
    const mode = root.dataset.mode;
    const isEdit = mode === "edit";

    const flashEl = document.getElementById("variant-form-flash");
    const rowsContainer = document.getElementById("attribute-rows");
    const addBtn = document.getElementById("attr-add-btn");
    const saveBtn = document.getElementById("variant-save-btn");
    const priceInput = document.getElementById("variant-price");
    const stockInput = document.getElementById("variant-stock");
    const quickTipAlert = document.getElementById("quick-tip-alert");
    const quickTipAlertEdit = document.getElementById("quick-tip-alert-edit");

    // Auto-hide tip alerts
    if (quickTipAlert) {
        setTimeout(() => {
            quickTipAlert.style.transition = "opacity 0.4s ease";
            quickTipAlert.style.opacity = "0";
            setTimeout(() => {
                quickTipAlert.style.display = "none";
            }, 400);
        }, 30000);
    }

    if (quickTipAlertEdit) {
        setTimeout(() => {
            quickTipAlertEdit.style.transition = "opacity 0.4s ease";
            quickTipAlertEdit.style.opacity = "0";
            setTimeout(() => {
                quickTipAlertEdit.style.display = "none";
            }, 400);
        }, 15000);
    }

    function showFlash(message, type) {
        if (!flashEl) return;
        flashEl.style.display = "flex";
        flashEl.className = `alert ${type === "error" ? "alert-error" : "alert-success"}`;
        flashEl.innerHTML = `<i class="bi ${type === "error" ? "bi-exclamation-circle-fill" : "bi-check-circle-fill"}"></i><div class="alert-content">${message}</div>`;
    }

    function createRow() {
        const row = document.createElement("div");
        row.className = "attribute-row";
        row.style.cssText = "display:flex; gap:1rem; align-items:center; margin-bottom:0.75rem;";
        row.innerHTML = `
            <input type="text" class="form-input attr-name" style="flex:1;" placeholder="Tên (VD: Màu sắc)" maxlength="100" />
            <input type="text" class="form-input attr-values" style="flex:1;" placeholder="${isEdit ? "Giá trị" : "Giá trị (VD: Đỏ, Xanh, Đen)"}" maxlength="500" />
            <button type="button" class="soft-action attr-remove" style="color:#b42318; border-color:#fda29b;">Xóa</button>
        `;
        return row;
    }

    if (addBtn) {
        addBtn.addEventListener("click", () => {
            rowsContainer.appendChild(createRow());
        });
    }

    rowsContainer.addEventListener("click", (event) => {
        const removeBtn = event.target.closest(".attr-remove");
        if (!removeBtn) return;

        if (rowsContainer.querySelectorAll(".attribute-row").length <= 1) {
            rowsContainer.querySelectorAll(".attr-name, .attr-values").forEach((input) => (input.value = ""));
            return;
        }

        removeBtn.closest(".attribute-row").remove();
    });

    function collectAttributes() {
        const attributes = [];
        rowsContainer.querySelectorAll(".attribute-row").forEach((row) => {
            const name = row.querySelector(".attr-name").value.trim();
            const rawValues = row.querySelector(".attr-values").value.trim();
            if (!name || !rawValues) return;

            const values = isEdit
                ? [rawValues]
                : rawValues.split(",").map((v) => v.trim()).filter((v) => v.length > 0);

            if (values.length > 0) {
                attributes.push({ name, values });
            }
        });
        return attributes;
    }

    async function save() {
        const attributes = collectAttributes();
        if (attributes.length === 0) {
            showFlash("Vui lòng nhập ít nhất một thuộc tính và giá trị.", "error");
            return;
        }

        const payload = {
            attributes,
            price: Number(priceInput.value || 0),
            stockQuantity: Number(stockInput.value || 0)
        };

        const url = isEdit
            ? `/Admin/Products/${productId}/Variants/${variantId}/Full`
            : `/Admin/Products/${productId}/Variants/AddBatch`;
        const method = isEdit ? "PUT" : "POST";

        saveBtn.disabled = true;
        try {
            const response = await fetch(url, {
                method,
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            const text = await response.text();
            let data = null;
            if (text) {
                try { data = JSON.parse(text); } catch { data = null; }
            }

            if (!response.ok) {
                const message = readErrorMessage(data) || "Không thể lưu biến thể. Vui lòng kiểm tra dữ liệu.";
                showFlash(message, "error");
                return;
            }

            window.location.href = `/Admin/ProductVariants/Index?productId=${productId}`;
        } catch {
            showFlash("Không thể kết nối máy chủ. Vui lòng thử lại.", "error");
        } finally {
            saveBtn.disabled = false;
        }
    }

    function readErrorMessage(data) {
        if (!data) return null;
        if (data.message) return data.message;
        if (data.errors) {
            const messages = [];
            Object.keys(data.errors).forEach((key) => {
                const arr = data.errors[key];
                if (Array.isArray(arr)) arr.forEach((item) => messages.push(item));
            });
            if (messages.length) return messages.join(" ");
        }
        if (data.title) return data.title;
        return null;
    }

    saveBtn.addEventListener("click", save);
})();
