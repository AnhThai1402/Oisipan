(function () {
    "use strict";

    window.togglePasswordVisibility = function (button) {
        const field = button.closest(".password-field");
        const input = field && field.querySelector(".password-input");
        const icon = button.querySelector("i");

        if (!input) {
            return;
        }

        const isHidden = input.type === "password";
        input.type = isHidden ? "text" : "password";
        button.setAttribute("aria-label", isHidden ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        button.setAttribute("aria-pressed", String(isHidden));

        if (icon) {
            icon.classList.toggle("bi-eye", !isHidden);
            icon.classList.toggle("bi-eye-slash", isHidden);
        }
    };

    window.initializeFormValidation = function () {
        const controls = document.querySelectorAll(".form-control");

        controls.forEach(function (control) {
            control.addEventListener("invalid", function () {
                control.classList.add("is-invalid");
            });

            control.addEventListener("input", function () {
                if (control.checkValidity()) {
                    control.classList.remove("is-invalid");
                }
            });
        });
    };

    window.initializeFormSubmission = function () {
        const forms = document.querySelectorAll(".login-form, .register-form, .forgot-password-form");

        forms.forEach(function (form) {
            form.addEventListener("submit", function () {
                if (!form.checkValidity()) {
                    return;
                }

                const button = form.querySelector(".submit-btn");
                if (!button) {
                    return;
                }

                button.disabled = true;
                button.dataset.originalText = button.textContent;
                button.textContent = "Đang xử lý...";
            });
        });
    };
})();
