(function () {
    const root = document.getElementById("variants-root");
    if (!root) {
        return;
    }

    const productId = root.dataset.productId;
    const flashEl = document.getElementById("variant-flash");
    const variantTbody = document.getElementById("variant-tbody");

    function showFlash(message, type) {
        if (!flashEl) return;
        flashEl.style.display = "flex";
        flashEl.className = "alert " + (type === "error" ? "alert-error" : "alert-success");
        flashEl.innerHTML = "<i class=\"bi " + (type === "error" ? "bi-exclamation-circle-fill" : "bi-check-circle-fill") + "\"></i><div class=\"alert-content\"><strong>" + message + "</strong></div>";
    }

    function clearFlash() {
        if (!flashEl) return;
        flashEl.style.display = "none";
        flashEl.innerHTML = "";
    }

    function formatCurrency(value) {
        return Number(value || 0).toLocaleString("vi-VN") + " đ";
    }

    function readErrorMessage(data) {
        if (!data) return null;
        if (data.message) return data.message;
        if (data.errors) {
            const messages = [];
            Object.keys(data.errors).forEach(function (key) {
                const arr = data.errors[key];
                if (Array.isArray(arr)) arr.forEach(function (item) { messages.push(item); });
            });
            if (messages.length) return messages.join(" ");
        }
        if (data.title) return data.title;
        return null;
    }

    function escapeHtml(text) {
        if (!text) return "";
        var map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    function renderVariants(variants) {
        if (!variants || !variants.length) {
            variantTbody.innerHTML = '<tr><td colspan="5" class="text-muted">Chưa có biến thể. Hãy bấm Thêm biến thể.</td></tr>';
            return;
        }

        variantTbody.innerHTML = variants.map(function (variant) {
            const badges = (variant.variantValues || [])
                .map(function (item) { 
                    return '<span class="badge-attribute">' + escapeHtml(item.optionName) + ': ' + escapeHtml(item.valueName) + '</span>'; 
                })
                .join(" ");

            return '' +
                '<tr data-variant-id="' + variant.productVariantId + '">' +
                    '<td>' + escapeHtml(variant.sku) + '</td>' +
                    '<td>' + (badges || "Mặc định") + '</td>' +
                    '<td>' + formatCurrency(variant.price) + '</td>' +
                    '<td>' + (variant.stockQuantity != null ? variant.stockQuantity : 0) + '</td>' +
                    '<td>' +
                        '<div class="action-buttons">' +
                            '<a class="icon-action js-variant-edit" href="/Admin/Products/' + variant.productId + '/Variants/' + variant.productVariantId + '/Edit" title="Sửa"><i class="bi bi-pencil"></i></a>' +
                            '<button type="button" class="icon-action danger js-variant-delete" data-variant-id="' + variant.productVariantId + '" title="Xóa"><i class="bi bi-trash"></i></button>' +
                        '</div>' +
                    '</td>' +
                '</tr>';
        }).join("");
    }

    function loadData() {
        variantTbody.innerHTML = '<tr><td colspan="5" class="text-muted">Đang tải...</td></tr>';
        fetch("/Admin/Products/" + productId + "/Variants/Data", {
            method: "GET",
            headers: { "Accept": "application/json; charset=utf-8" }
        })
            .then(function (response) {
                if (!response.ok) throw new Error("Network error");
                return response.json();
            })
            .then(function (data) {
                console.log("Loaded variants:", data.variants);
                renderVariants(data.variants || []);
            })
            .catch(function (error) {
                console.error("Error loading variants:", error);
                variantTbody.innerHTML = '<tr><td colspan="5" class="text-muted">Không thể tải danh sách biến thể.</td></tr>';
            });
    }

    variantTbody.addEventListener("click", function (event) {
        const deleteButton = event.target.closest(".js-variant-delete");
        if (!deleteButton) return;

        if (!window.confirm("Bạn có chắc muốn xóa biến thể này?")) return;

        clearFlash();
        fetch("/Admin/Products/" + productId + "/Variants/" + deleteButton.dataset.variantId, { method: "DELETE" })
            .then(function (response) {
                if (!response.ok) {
                    return response.text().then(function (text) {
                        let data = null;
                        if (text) {
                            try { data = JSON.parse(text); } catch (e) { data = null; }
                        }
                        throw new Error(readErrorMessage(data) || "Không thể xóa biến thể này.");
                    });
                }
                showFlash("Xóa biến thể thành công.", "success");
                return loadData();
            })
            .catch(function (error) {
                showFlash(error.message, "error");
            });
    });

    loadData();
})();
