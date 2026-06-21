(function () {
    "use strict";

    const editorRoot = document.querySelector("[data-rich-editor]");
    const source = document.querySelector(".rich-editor-source");
    const form = editorRoot && editorRoot.closest("form");

    if (!editorRoot || !source || !form) {
        return;
    }

    const editor = editorRoot.querySelector(".rich-editor-content");
    const toolbar = editorRoot.querySelector(".rich-editor-toolbar");
    const formatSelect = toolbar.querySelector("[data-block-format]");
    let validationAttempted = false;

    function looksLikeHtml(value) {
        return /<\/?[a-z][\s\S]*>/i.test(value);
    }

    function loadContent() {
        const value = source.value.trim();
        if (!value) {
            editor.innerHTML = "";
            return;
        }

        if (looksLikeHtml(value)) {
            editor.innerHTML = value;
            return;
        }

        editor.innerHTML = "";
        value.split(/\r?\n\r?\n/).forEach(function (paragraph) {
            const element = document.createElement("p");
            element.textContent = paragraph.replace(/\r?\n/g, " ");
            editor.appendChild(element);
        });
    }

    function syncContent() {
        const hasText = editor.textContent.trim().length > 0;
        const hasMedia = editor.querySelector("img, video, iframe");
        source.value = hasText || hasMedia ? editor.innerHTML.trim() : "";
        source.dispatchEvent(new Event("input", { bubbles: true }));
        if (source.value) {
            editorRoot.classList.remove("input-validation-error");
        } else if (validationAttempted) {
            editorRoot.classList.add("input-validation-error");
        }
    }

    function runCommand(command, value) {
        editor.focus();
        document.execCommand(command, false, value || null);
        syncContent();
    }

    toolbar.addEventListener("mousedown", function (event) {
        if (event.target.closest("button")) {
            event.preventDefault();
        }
    });

    toolbar.addEventListener("click", function (event) {
        const button = event.target.closest("[data-command]");
        if (!button) {
            return;
        }

        const command = button.dataset.command;
        let value = button.dataset.value || null;

        if (command === "createLink") {
            value = window.prompt("Nhập địa chỉ liên kết (https://...)");
            if (!value) {
                return;
            }
        }

        if (command === "insertImage") {
            value = window.prompt("Nhập đường dẫn ảnh (https://...)");
            if (!value) {
                return;
            }
        }

        runCommand(command, value);
    });

    formatSelect.addEventListener("change", function () {
        runCommand("formatBlock", formatSelect.value);
        formatSelect.value = "p";
    });

    editor.addEventListener("input", syncContent);
    editor.addEventListener("blur", syncContent);
    editor.addEventListener("paste", function () {
        window.setTimeout(syncContent, 0);
    });

    form.addEventListener("submit", function (event) {
        validationAttempted = true;
        syncContent();
        if (source.value) {
            return;
        }

        event.preventDefault();
        editorRoot.classList.add("input-validation-error");
        editor.focus();
    });

    loadContent();
    syncContent();
})();
