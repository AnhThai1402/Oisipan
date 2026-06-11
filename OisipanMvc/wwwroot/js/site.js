document.querySelectorAll(".toggle-password").forEach((button) => {
    button.addEventListener("click", () => {
        const input = button.parentElement?.querySelector(".password-input");
        if (!input) {
            return;
        }

        const isPassword = input.type === "password";
        input.type = isPassword ? "text" : "password";
        button.textContent = isPassword ? "Ẩn" : "Hiện";
    });
});

const loginForm = document.getElementById("loginForm");
if (loginForm) {
    const emailInput = document.getElementById("Email");
    const passwordInput = document.getElementById("Password");
    const rememberInput = document.getElementById("rememberPassword");

    const savedEmail = localStorage.getItem("oisipan_email");
    const savedPassword = localStorage.getItem("oisipan_password");
    if (savedEmail && savedPassword && emailInput && passwordInput && rememberInput) {
        emailInput.value = savedEmail;
        passwordInput.value = savedPassword;
        rememberInput.checked = true;
    }

    loginForm.addEventListener("submit", () => {
        if (!emailInput || !passwordInput || !rememberInput) {
            return;
        }

        if (rememberInput.checked) {
            localStorage.setItem("oisipan_email", emailInput.value);
            localStorage.setItem("oisipan_password", passwordInput.value);
        } else {
            localStorage.removeItem("oisipan_email");
            localStorage.removeItem("oisipan_password");
        }
    });
}
