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
 * Add smooth transitions and AJAX when form is submitted
 */
function initializeFormSubmission() {
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', async function(e) {
            e.preventDefault(); // Prevent native reload

            // Client-side empty validation check first
            let hasEmpty = false;
            const inputs = form.querySelectorAll('input:not([type="hidden"]):not([type="checkbox"])');
            inputs.forEach(input => {
                if (!input.value.trim() && input.id !== 'Address') { // Address might be optional depending on logic, but others are required
                    hasEmpty = true;
                    const errorSpan = input.closest('.form-group')?.querySelector('.field-validation-error');
                    if (errorSpan) {
                        errorSpan.textContent = 'Vui lòng không bỏ trống trường này.';
                        errorSpan.style.display = 'block';
                    }
                    input.style.borderColor = '#dc3545';
                    input.style.background = 'rgba(220, 53, 69, 0.05)';
                }
            });

            if (hasEmpty) {
                return; // Stop here if empty fields exist
            }

            const submitBtn = this.querySelector('.submit-btn');
            const originalText = submitBtn ? submitBtn.textContent : '';
            if (submitBtn) {
                submitBtn.style.opacity = '0.7';
                submitBtn.style.pointerEvents = 'none';
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true" style="margin-right: 8px;"></span> Đang xử lý...';
            }

            const formData = new FormData(form);
            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (response.redirected) {
                    // Success! Follow the redirect
                    window.location.href = response.url;
                    return;
                }

                // If not redirected, the server likely returned the view with validation errors
                const html = await response.text();
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');
                
                const newForm = doc.querySelector('form');
                if (newForm) {
                    form.innerHTML = newForm.innerHTML;
                    // Re-initialize visual validation for the newly injected inputs
                    initializeFormValidation();
                } else {
                    // Fallback
                    location.reload();
                }
            } catch (err) {
                console.error('Lỗi khi gửi form:', err);
            } finally {
                if (submitBtn) {
                    submitBtn.style.opacity = '1';
                    submitBtn.style.pointerEvents = 'auto';
                    submitBtn.textContent = originalText;
                }
            }
        });
    });
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeFormValidation();
    initializeFormSubmission();
});
