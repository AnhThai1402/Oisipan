(function () {
    const root = document.getElementById("variants-root");
    if (!root) {
        return;
    }

    const productId = root.dataset.productId;
    const state = {
        options: [],
        variants: [],
        selectedOptionId: null,
        selectedVariantIds: new Set(),
        variantSearch: "",
        variantStatusFilter: "all",
        pendingRequests: 0,
        isGenerating: false
    };

    const flashEl = document.getElementById("variant-flash");
    const optionTbody = document.getElementById("option-tbody");
    const valueTbody = document.getElementById("value-tbody");
    const variantTbody = document.getElementById("variant-tbody");
    const valueTitle = document.getElementById("value-title");

    const optionForm = document.getElementById("option-form");
    const optionIdInput = document.getElementById("option-id");
    const optionNameInput = document.getElementById("option-name");
    const optionCancelBtn = document.getElementById("option-cancel");

    const valueForm = document.getElementById("value-form");
    const valueIdInput = document.getElementById("value-id");
    const valueOptionIdInput = document.getElementById("value-option-id");
    const valueNameInput = document.getElementById("value-name");
    const valueAdditionalPriceInput = document.getElementById("value-additional-price");
    const valueCancelBtn = document.getElementById("value-cancel");

    const generateBtn = document.getElementById("generate-btn");
    const selectAllVariantsCheckbox = document.getElementById("variant-select-all");
    const bulkStatusActiveBtn = document.getElementById("bulk-status-active");
    const bulkStatusInactiveBtn = document.getElementById("bulk-status-inactive");
    const bulkDeleteBtn = document.getElementById("bulk-delete");
    const bulkStockDeltaInput = document.getElementById("bulk-stock-delta");
    const bulkStockApplyBtn = document.getElementById("bulk-stock-apply");
    const variantSearchInput = document.getElementById("variant-search");
    const variantStatusFilterInput = document.getElementById("variant-status-filter");
    const variantsLoadingEl = document.getElementById("variants-loading");

    function showFlash(message, type) {
        if (!flashEl) return;
        flashEl.style.display = "flex";
        flashEl.className = `alert ${type === "error" ? "alert-error" : "alert-success"}`;
        flashEl.innerHTML = `<i class="bi ${type === "error" ? "bi-exclamation-circle-fill" : "bi-check-circle-fill"}"></i><div class="alert-content"><strong>${message}</strong></div>`;
    }

    function clearFlash() {
        if (!flashEl) return;
        flashEl.style.display = "none";
        flashEl.innerHTML = "";
    }

    function setRequestState(isBusy, message) {
        if (variantsLoadingEl) {
            variantsLoadingEl.style.display = isBusy ? "inline-flex" : "none";
            variantsLoadingEl.innerHTML = `<i class="bi bi-arrow-repeat"></i> ${message || "Đang xử lý..."}`;
        }

        document.querySelectorAll("#variants-root button").forEach((button) => {
            button.disabled = isBusy;
        });
    }

    async function withRequestState(action, message) {
        state.pendingRequests += 1;
        setRequestState(true, message);
        try {
            return await action();
        } finally {
            state.pendingRequests = Math.max(0, state.pendingRequests - 1);
            if (state.pendingRequests === 0) {
                setRequestState(false);
            }
        }
    }

    async function apiRequest(url, method, body) {
        return withRequestState(async () => {
            const response = await fetch(url, {
                method,
                headers: {
                    "Content-Type": "application/json"
                },
                body: body ? JSON.stringify(body) : undefined
            });

            const text = await response.text();
            let data = null;
            if (text) {
                try {
                    data = JSON.parse(text);
                } catch {
                    data = null;
                }
            }

            if (!response.ok) {
                const fallbackMessage = response.status >= 500
                    ? "Không thể xử lý yêu cầu lúc này. Vui lòng thử lại sau."
                    : "Không thể thực hiện thao tác. Vui lòng kiểm tra dữ liệu.";
                const errorMessage = readErrorMessage(data) || (text && text.length < 200 ? text : null);
                throw new Error(resolveErrorMessage(errorMessage, fallbackMessage));
            }

            return data;
        }, "Đang xử lý yêu cầu...");
    }

    function readErrorMessage(data) {
        if (!data) return null;
        if (data.message) return data.message;
        if (data.detail) return data.detail;
        if (data.errors) {
            const messages = [];
            Object.keys(data.errors).forEach((key) => {
                const arr = data.errors[key];
                if (Array.isArray(arr)) {
                    arr.forEach((item) => messages.push(item));
                }
            });
            if (messages.length) {
                return messages.join(" ");
            }
        }
        if (data.title) return data.title;
        return null;
    }

    function resolveErrorMessage(message, fallbackMessage) {
        if (!message) {
            return fallbackMessage;
        }

        const normalized = message.trim().toLowerCase();
        if (
            normalized === "an error occurred." ||
            normalized === "an error occurred" ||
            normalized === "an error has occurred." ||
            normalized.includes("an error occurred while processing your request")
        ) {
            return fallbackMessage;
        }

        return message;
    }

    function getSelectedOption() {
        return state.options.find((item) => item.productOptionId === state.selectedOptionId) || null;
    }

    function formatCurrency(value) {
        return `${Number(value || 0).toLocaleString("vi-VN")}đ`;
    }

    function renderOptions() {
        if (!optionTbody) return;
        if (!state.options.length) {
            optionTbody.innerHTML = '<tr><td colspan="3" class="text-muted">Chưa có tùy chọn nào.</td></tr>';
            return;
        }

        optionTbody.innerHTML = state.options.map((option) => {
            const activeClass = option.productOptionId === state.selectedOptionId ? " style=\"background:#f5f7ff;\"" : "";
            return `<tr data-option-id="${option.productOptionId}"${activeClass}>
                <td>${option.optionName}</td>
                <td>${(option.productValues || []).length}</td>
                <td>
                    <div class="action-buttons">
                        <button type="button" class="icon-action js-option-edit" data-option-id="${option.productOptionId}" title="Sửa"><i class="bi bi-pencil"></i></button>
                        <button type="button" class="icon-action danger js-option-delete" data-option-id="${option.productOptionId}" title="Xóa"><i class="bi bi-trash"></i></button>
                    </div>
                </td>
            </tr>`;
        }).join("");
    }

    function renderValues() {
        const selected = getSelectedOption();
        if (!selected) {
            valueTitle.textContent = "Giá trị tùy chọn";
            valueTbody.innerHTML = '<tr><td colspan="3" class="text-muted">Chọn một tùy chọn để quản lý giá trị.</td></tr>';
            return;
        }

        valueTitle.textContent = `Giá trị - ${selected.optionName}`;
        const values = selected.productValues || [];
        if (!values.length) {
            valueTbody.innerHTML = '<tr><td colspan="3" class="text-muted">Chưa có giá trị nào.</td></tr>';
            return;
        }

        valueTbody.innerHTML = values.map((value) => `
            <tr>
                <td>${value.valueName}</td>
                <td>${formatCurrency(value.additionalPrice)}</td>
                <td>
                    <div class="action-buttons">
                        <button type="button" class="icon-action js-value-edit" data-value-id="${value.productValueId}" data-option-id="${selected.productOptionId}" title="Sửa"><i class="bi bi-pencil"></i></button>
                        <button type="button" class="icon-action danger js-value-delete" data-value-id="${value.productValueId}" title="Xóa"><i class="bi bi-trash"></i></button>
                    </div>
                </td>
            </tr>
        `).join("");
    }

    function getFilteredVariants() {
        const search = state.variantSearch.trim().toLowerCase();
        return state.variants.filter((variant) => {
            const variantName = (variant.variantValues || [])
                .map((item) => `${item.optionName}: ${item.valueName}`)
                .join(" | ") || "Mặc định";
            const statusMatch = state.variantStatusFilter === "all"
                || (state.variantStatusFilter === "active" && variant.isActive)
                || (state.variantStatusFilter === "inactive" && !variant.isActive);
            const searchMatch = !search
                || variantName.toLowerCase().includes(search)
                || (variant.sku || "").toLowerCase().includes(search);
            return statusMatch && searchMatch;
        });
    }

    function renderVariants() {
        if (!variantTbody) return;
        if (!state.variants.length) {
            variantTbody.innerHTML = '<tr><td colspan="7" class="text-muted">Chưa có variants. Hãy bấm Generate variants.</td></tr>';
            return;
        }

        const filteredVariants = getFilteredVariants();

        if (!filteredVariants.length) {
            variantTbody.innerHTML = '<tr><td colspan="7" class="text-muted">Không tìm thấy variant phù hợp bộ lọc.</td></tr>';
            return;
        }

        variantTbody.innerHTML = filteredVariants.map((variant) => {
            const variantName = (variant.variantValues || [])
                .map((item) => `${item.optionName}: ${item.valueName}`)
                .join(" | ") || "Mặc định";
            const checked = state.selectedVariantIds.has(variant.productVariantId) ? "checked" : "";
            return `
                <tr data-variant-id="${variant.productVariantId}">
                    <td><input type="checkbox" class="js-variant-select" data-variant-id="${variant.productVariantId}" ${checked} /></td>
                    <td>${variantName}</td>
                    <td><input class="form-input js-variant-sku" data-variant-id="${variant.productVariantId}" value="${variant.sku || ""}" placeholder="SKU" maxlength="50" /></td>
                    <td><input type="number" min="0" step="1000" class="form-input js-variant-price" data-variant-id="${variant.productVariantId}" value="${variant.price ?? 0}" /></td>
                    <td><input type="number" min="0" step="1" class="form-input js-variant-stock" data-variant-id="${variant.productVariantId}" value="${variant.stockQuantity ?? 0}" /></td>
                    <td>
                        <select class="form-input js-variant-status" data-variant-id="${variant.productVariantId}">
                            <option value="true" ${variant.isActive ? "selected" : ""}>Đang bán</option>
                            <option value="false" ${!variant.isActive ? "selected" : ""}>Ẩn</option>
                        </select>
                    </td>
                    <td>
                        <div class="action-buttons">
                            <button type="button" class="icon-action js-variant-save" data-variant-id="${variant.productVariantId}" title="Lưu"><i class="bi bi-check-lg"></i></button>
                            <button type="button" class="icon-action danger js-variant-delete" data-variant-id="${variant.productVariantId}" title="Xóa"><i class="bi bi-trash"></i></button>
                        </div>
                    </td>
                </tr>
            `;
        }).join("");
    }

    function syncSelectAllState() {
        if (!selectAllVariantsCheckbox) return;
        const filteredIds = getFilteredVariants().map((item) => item.productVariantId);
        selectAllVariantsCheckbox.checked = filteredIds.length > 0 && filteredIds.every((id) => state.selectedVariantIds.has(id));
    }

    function getSelectedVariantIds() {
        return Array.from(state.selectedVariantIds);
    }

    async function executeBulkAction(url, payload, successMessage) {
        const selectedIds = getSelectedVariantIds();
        if (!selectedIds.length) {
            showFlash("Vui lòng chọn ít nhất một variant.", "error");
            return;
        }

        try {
            await apiRequest(url, "POST", {
                variantIds: selectedIds,
                ...payload
            });
            showFlash(successMessage, "success");
            await loadData();
        } catch (error) {
            showFlash(error.message, "error");
        }
    }

    function resetOptionForm() {
        optionIdInput.value = "";
        optionNameInput.value = "";
        optionCancelBtn.style.display = "none";
    }

    function resetValueForm() {
        valueIdInput.value = "";
        valueOptionIdInput.value = state.selectedOptionId || "";
        valueNameInput.value = "";
        valueAdditionalPriceInput.value = "0";
        valueCancelBtn.style.display = "none";
    }

    async function loadData(keepSelection) {
        optionTbody.innerHTML = '<tr><td colspan="3" class="text-muted">Đang tải...</td></tr>';
        valueTbody.innerHTML = '<tr><td colspan="3" class="text-muted">Đang tải...</td></tr>';
        variantTbody.innerHTML = '<tr><td colspan="7" class="text-muted">Đang tải...</td></tr>';

        const data = await apiRequest(`/Admin/Products/${productId}/Variants/Data`, "GET");
        state.options = data.options || [];
        state.variants = data.variants || [];

        const validVariantIds = new Set(state.variants.map((item) => item.productVariantId));
        state.selectedVariantIds = new Set(
            Array.from(state.selectedVariantIds).filter((id) => validVariantIds.has(id))
        );

        if (keepSelection && keepSelection.selectedOptionId && state.options.some(o => o.productOptionId === keepSelection.selectedOptionId)) {
            state.selectedOptionId = keepSelection.selectedOptionId;
        } else {
            state.selectedOptionId = state.options[0]?.productOptionId || null;
        }

        renderOptions();
        renderValues();
        renderVariants();
        syncSelectAllState();
        resetValueForm();
    }

    optionForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearFlash();

        const optionName = optionNameInput.value.trim();
        if (!optionName) {
            showFlash("Vui lòng nhập tên tùy chọn.", "error");
            return;
        }

        try {
            if (optionIdInput.value) {
                await apiRequest(`/Admin/Products/${productId}/Variants/Options/${optionIdInput.value}`, "PUT", { optionName });
                showFlash("Cập nhật tùy chọn thành công.", "success");
            } else {
                await apiRequest(`/Admin/Products/${productId}/Variants/Options`, "POST", { optionName });
                showFlash("Thêm tùy chọn thành công.", "success");
            }
            const selectedOptionId = optionIdInput.value || state.selectedOptionId;
            resetOptionForm();
            await loadData({ selectedOptionId });
        } catch (error) {
            showFlash(error.message, "error");
        }
    });

    optionCancelBtn.addEventListener("click", () => {
        resetOptionForm();
    });

    optionTbody.addEventListener("click", async (event) => {
        const editButton = event.target.closest(".js-option-edit");
        const deleteButton = event.target.closest(".js-option-delete");
        const row = event.target.closest("tr[data-option-id]");

        if (row && !editButton && !deleteButton) {
            state.selectedOptionId = row.dataset.optionId;
            renderOptions();
            renderValues();
            resetValueForm();
            return;
        }

        if (editButton) {
            const option = state.options.find((item) => item.productOptionId === editButton.dataset.optionId);
            if (!option) return;
            optionIdInput.value = option.productOptionId;
            optionNameInput.value = option.optionName;
            optionCancelBtn.style.display = "inline-flex";
            return;
        }

        if (deleteButton) {
            if (!window.confirm("Bạn có chắc muốn xóa tùy chọn này?")) return;
            try {
                await apiRequest(`/Admin/Products/${productId}/Variants/Options/${deleteButton.dataset.optionId}`, "DELETE");
                showFlash("Xóa tùy chọn thành công.", "success");
                await loadData();
            } catch (error) {
                showFlash(resolveErrorMessage(
                    error?.message,
                    "Không thể xóa tùy chọn này vì đang được dùng trong biến thể sản phẩm. Vui lòng xóa hoặc cập nhật các biến thể liên quan trước."
                ), "error");
            }
        }
    });

    valueForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearFlash();

        const selected = getSelectedOption();
        if (!selected) {
            showFlash("Vui lòng chọn tùy chọn trước khi thêm giá trị.", "error");
            return;
        }

        const valueName = valueNameInput.value.trim();
        const additionalPrice = Number(valueAdditionalPriceInput.value || 0);
        if (!valueName) {
            showFlash("Vui lòng nhập tên giá trị.", "error");
            return;
        }

        try {
            if (valueIdInput.value) {
                await apiRequest(`/Admin/Products/${productId}/Variants/Values/${valueIdInput.value}`, "PUT", {
                    productOptionId: valueOptionIdInput.value,
                    valueName,
                    additionalPrice
                });
                showFlash("Cập nhật giá trị thành công.", "success");
            } else {
                await apiRequest(`/Admin/Products/${productId}/Variants/Options/${selected.productOptionId}/Values`, "POST", {
                    valueName,
                    additionalPrice
                });
                showFlash("Thêm giá trị thành công.", "success");
            }

            const selectedOptionId = selected.productOptionId;
            resetValueForm();
            await loadData({ selectedOptionId });
        } catch (error) {
            showFlash(error.message, "error");
        }
    });

    valueCancelBtn.addEventListener("click", () => {
        resetValueForm();
    });

    valueTbody.addEventListener("click", async (event) => {
        const editButton = event.target.closest(".js-value-edit");
        const deleteButton = event.target.closest(".js-value-delete");
        if (!editButton && !deleteButton) return;

        const selected = getSelectedOption();
        if (!selected) return;

        if (editButton) {
            const value = (selected.productValues || []).find((item) => item.productValueId === editButton.dataset.valueId);
            if (!value) return;
            valueIdInput.value = value.productValueId;
            valueOptionIdInput.value = editButton.dataset.optionId;
            valueNameInput.value = value.valueName;
            valueAdditionalPriceInput.value = value.additionalPrice ?? 0;
            valueCancelBtn.style.display = "inline-flex";
            return;
        }

        if (deleteButton) {
            if (!window.confirm("Bạn có chắc muốn xóa giá trị này?")) return;
            try {
                await apiRequest(`/Admin/Products/${productId}/Variants/Values/${deleteButton.dataset.valueId}`, "DELETE");
                showFlash("Xóa giá trị thành công.", "success");
                await loadData({ selectedOptionId: selected.productOptionId });
            } catch (error) {
                showFlash(error.message, "error");
            }
        }
    });

    if (selectAllVariantsCheckbox) {
        selectAllVariantsCheckbox.addEventListener("change", (event) => {
            const filteredIds = getFilteredVariants().map((item) => item.productVariantId);
            if (event.target.checked) {
                filteredIds.forEach((id) => state.selectedVariantIds.add(id));
            } else {
                filteredIds.forEach((id) => state.selectedVariantIds.delete(id));
            }
            renderVariants();
        });
    }

    variantTbody.addEventListener("change", (event) => {
        const checkbox = event.target.closest(".js-variant-select");
        if (!checkbox) return;

        if (checkbox.checked) {
            state.selectedVariantIds.add(checkbox.dataset.variantId);
        } else {
            state.selectedVariantIds.delete(checkbox.dataset.variantId);
        }

        syncSelectAllState();
    });

    variantTbody.addEventListener("click", async (event) => {
        const saveButton = event.target.closest(".js-variant-save");
        const deleteButton = event.target.closest(".js-variant-delete");
        if (!saveButton && !deleteButton) return;

        const variantId = (saveButton || deleteButton).dataset.variantId;
        if (!variantId) return;

        if (saveButton) {
            const skuInput = variantTbody.querySelector(`.js-variant-sku[data-variant-id="${variantId}"]`);
            const priceInput = variantTbody.querySelector(`.js-variant-price[data-variant-id="${variantId}"]`);
            const stockInput = variantTbody.querySelector(`.js-variant-stock[data-variant-id="${variantId}"]`);
            const statusInput = variantTbody.querySelector(`.js-variant-status[data-variant-id="${variantId}"]`);

            try {
                await apiRequest(`/Admin/Products/${productId}/Variants/${variantId}`, "PUT", {
                    sku: skuInput?.value?.trim() || null,
                    price: Number(priceInput?.value || 0),
                    stockQuantity: Number(stockInput?.value || 0),
                    isActive: (statusInput?.value || "true") === "true"
                });
                showFlash("Cập nhật variant thành công.", "success");
                await loadData();
            } catch (error) {
                showFlash(error.message, "error");
            }
            return;
        }

        if (!window.confirm("Bạn có chắc muốn xóa variant này?")) return;
        try {
            await apiRequest(`/Admin/Products/${productId}/Variants/${variantId}`, "DELETE");
            showFlash("Xóa variant thành công.", "success");
            await loadData();
        } catch (error) {
            showFlash(error.message, "error");
        }
    });

    bulkStatusActiveBtn?.addEventListener("click", async () => {
        clearFlash();
        await executeBulkAction(`/Admin/Products/${productId}/Variants/Bulk/Status`, { isActive: true }, "Đã bật trạng thái bán cho variants đã chọn.");
    });

    bulkStatusInactiveBtn?.addEventListener("click", async () => {
        clearFlash();
        await executeBulkAction(`/Admin/Products/${productId}/Variants/Bulk/Status`, { isActive: false }, "Đã ẩn variants đã chọn.");
    });

    bulkDeleteBtn?.addEventListener("click", async () => {
        clearFlash();
        if (!window.confirm("Bạn có chắc muốn xóa các variants đã chọn?")) return;
        await executeBulkAction(`/Admin/Products/${productId}/Variants/Bulk/Delete`, {}, "Đã xóa variants đã chọn.");
    });

    bulkStockApplyBtn?.addEventListener("click", async () => {
        clearFlash();
        const delta = Number(bulkStockDeltaInput?.value || 0);
        if (!Number.isInteger(delta) || delta === 0) {
            showFlash("Vui lòng nhập số điều chỉnh tồn kho khác 0.", "error");
            return;
        }

        await executeBulkAction(`/Admin/Products/${productId}/Variants/Bulk/StockAdjust`, { deltaQuantity: delta }, "Đã điều chỉnh tồn kho cho variants đã chọn.");
        if (bulkStockDeltaInput) {
            bulkStockDeltaInput.value = "";
        }
    });

    generateBtn.addEventListener("click", async () => {
        if (state.isGenerating) {
            return;
        }

        clearFlash();
        state.isGenerating = true;
        try {
            const data = await apiRequest(`/Admin/Products/${productId}/Variants/Generate`, "POST");
            state.variants = data?.variants || [];
            renderVariants();
            if (selectAllVariantsCheckbox) {
                selectAllVariantsCheckbox.checked = false;
            }
            showFlash("Đã generate variants thành công.", "success");
        } catch (error) {
            showFlash(error.message, "error");
        } finally {
            state.isGenerating = false;
        }
    });

    variantSearchInput?.addEventListener("input", () => {
        state.variantSearch = variantSearchInput.value || "";
        renderVariants();
        syncSelectAllState();
    });

    variantStatusFilterInput?.addEventListener("change", () => {
        state.variantStatusFilter = variantStatusFilterInput.value || "all";
        renderVariants();
        syncSelectAllState();
    });

    loadData().catch((error) => showFlash(error.message, "error"));
})();
