// ============================================
// Authentication Form Scripts
// ============================================

/**
 * Toggle password visibility (Bootstrap Icons)
 * @param {HTMLElement} button - The toggle button clicked
 */
function togglePasswordVisibility(button) {
    const input = button.previousElementSibling;
    const icon = button.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('bi-eye');
        icon.classList.add('bi-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('bi-eye-slash');
        icon.classList.add('bi-eye');
    }
}

/**
 * Toggle password visibility (Font Awesome - Legacy)
 * @param {HTMLElement} button - The toggle button clicked
 */
function togglePassword(button) {
    const input = button.parentElement.querySelector('[data-toggle="password"]');
    const icon = button.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
}

/**
 * Initialize form validation visual feedback
 */
function initializeFormValidation() {
    document.querySelectorAll('.form-control').forEach(input => {
        // On blur: check for errors and highlight
        input.addEventListener('blur', function() {
            const errorSpan = this.closest('.form-group')?.querySelector('.form-error');
            if (errorSpan && errorSpan.textContent.trim()) {
                this.style.borderColor = '#dc3545';
                this.style.background = 'rgba(220, 53, 69, 0.05)';
            }
        });

        // On focus: reset to normal state
        input.addEventListener('focus', function() {
            this.style.borderColor = '#2a8659';
            this.style.background = '#fff';
        });

        // On input: if there was an error and user starts typing, clear the error highlight
        input.addEventListener('input', function() {
            const errorSpan = this.closest('.form-group')?.querySelector('.form-error');
            if (!errorSpan || !errorSpan.textContent.trim()) {
                this.style.borderColor = '#e8e8e8';
                this.style.background = '#f9f9f9';
            }
        });
    });
}

/**
 * Add smooth transitions when form is submitted
 */
function initializeFormSubmission() {
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function(e) {
            const submitBtn = this.querySelector('.submit-btn');
            if (submitBtn) {
                submitBtn.style.opacity = '0.7';
                submitBtn.style.pointerEvents = 'none';
            }
        });
    });
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeFormValidation();
    initializeFormSubmission();
});
