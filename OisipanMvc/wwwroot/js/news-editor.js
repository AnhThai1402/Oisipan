(function () {
    "use strict";

    const source = document.getElementById("news-content-editor");
    const form = source?.closest("form");

    if (!source || !form || !window.ClassicEditor) {
        return;
    }

    class NewsImageUploadAdapter {
        constructor(loader, uploadUrl, antiForgeryToken) {
            this.loader = loader;
            this.uploadUrl = uploadUrl;
            this.antiForgeryToken = antiForgeryToken;
            this.controller = new AbortController();
        }

        async upload() {
            const file = await this.loader.file;
            const body = new FormData();
            body.append("upload", file);

            const response = await fetch(this.uploadUrl, {
                method: "POST",
                body,
                headers: {
                    RequestVerificationToken: this.antiForgeryToken
                },
                signal: this.controller.signal
            });
            const result = await response.json();

            if (!response.ok || !result.url) {
                throw new Error(result.error?.message || "Không thể tải ảnh lên.");
            }

            return { default: result.url };
        }

        abort() {
            this.controller.abort();
        }
    }

    function configureImageUpload(editor) {
        const uploadUrl = source.dataset.uploadUrl;
        const antiForgeryToken =
            form.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

        editor.plugins.get("FileRepository").createUploadAdapter = loader =>
            new NewsImageUploadAdapter(loader, uploadUrl, antiForgeryToken);
    }

    window.ClassicEditor.create(source, {
        language: "vi",
        toolbar: {
            items: [
                "undo", "redo", "|",
                "heading", "|",
                "bold", "italic", "link", "|",
                "bulletedList", "numberedList", "blockQuote", "|",
                "insertTable", "imageUpload", "mediaEmbed"
            ],
            shouldNotGroupWhenFull: false
        },
        image: {
            toolbar: [
                "imageTextAlternative",
                "toggleImageCaption",
                "|",
                "imageStyle:inline",
                "imageStyle:block",
                "imageStyle:side",
                "|",
                "linkImage"
            ]
        },
        table: {
            contentToolbar: [
                "tableColumn",
                "tableRow",
                "mergeTableCells"
            ]
        }
    }).then(editor => {
        configureImageUpload(editor);

        const syncContent = () => {
            source.value = editor.getData();
            source.dispatchEvent(new Event("input", { bubbles: true }));
        };

        editor.model.document.on("change:data", syncContent);
        form.addEventListener("submit", syncContent);
    }).catch(error => {
        console.error("Không thể khởi tạo CKEditor:", error);
    });
})();
