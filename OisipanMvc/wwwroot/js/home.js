document.addEventListener('DOMContentLoaded', () => {
    const header = document.getElementById('main-header');
    const cartSidebar = document.getElementById('cart-sidebar');
    const openCartBtn = document.getElementById('open-cart-btn');
    const closeCartBtn = document.getElementById('close-cart-btn');
    const checkoutBtn = document.getElementById('cart-checkout-btn');
    const checkoutOverlay = document.getElementById('checkout-modal-overlay');
    const closeCheckoutBtn = document.getElementById('close-checkout-modal');
    const checkoutForm = document.getElementById('checkout-form');

    // Auth forms validation intercept
    const authForms = document.querySelectorAll('form.auth-form');
    authForms.forEach(form => {
        // Skip forgot password form which has its own handler
        if (form.id === 'forgot-form') return;
        
        form.addEventListener('submit', event => {
            const requiredInputs = form.querySelectorAll('input[required]');
            let isValid = true;
            requiredInputs.forEach(input => {
                if (!input.value.trim()) {
                    isValid = false;
                }
            });
            if (!isValid) {
                event.preventDefault();
                triggerToast('Vui lòng điền đầy đủ các trường bắt buộc.', 'warning');
            }
        });
    });

    const navLinks = document.querySelectorAll('#main-header nav a');
    const sections = ['hero', 'about', 'menu', 'news', 'footer'].map(id => document.getElementById(id)).filter(el => el != null);

    const onScroll = () => {
        if (header) {
            header.classList.toggle('scrolled', window.scrollY > 40);
        }

        if (sections.length > 0) {
            let current = '';
            sections.forEach(section => {
                const sectionTop = section.offsetTop;
                if (window.scrollY >= (sectionTop - 150)) {
                    current = section.getAttribute('id');
                }
            });

            if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 50) {
                const footerSection = sections.find(s => s.id === 'footer');
                if (footerSection) current = 'footer';
            }

            if (current) {
                navLinks.forEach(link => {
                    link.classList.remove('active');
                    if (link.getAttribute('href') && link.getAttribute('href').includes('#' + current)) {
                        link.classList.add('active');
                    }
                });
            }
        }
    };

    window.addEventListener('scroll', onScroll);
    onScroll(); // Trigger once on load

    openCartBtn?.addEventListener('click', () => cartSidebar?.classList.add('open'));
    closeCartBtn?.addEventListener('click', () => cartSidebar?.classList.remove('open'));

    checkoutBtn?.addEventListener('click', () => {
        if (checkoutBtn.disabled) {
            triggerToast('Giỏ hàng trống.', 'info');
            return;
        }

        cartSidebar?.classList.remove('open');
        const buyNowInput = document.getElementById('buy-now-product-id');
        if (buyNowInput) buyNowInput.value = '';
        const display = document.getElementById('checkout-total-display');
        if (display && display.hasAttribute('data-cart-total')) {
            const originalTotal = parseInt(display.getAttribute('data-cart-total') || '0', 10);
            display.textContent = originalTotal.toLocaleString('vi-VN') + 'đ';
        }
        checkoutOverlay?.classList.add('open');
    });

    closeCheckoutBtn?.addEventListener('click', () => closeCheckoutModal());
    checkoutOverlay?.addEventListener('click', event => {
        if (event.target === checkoutOverlay) closeCheckoutModal();
    });

    checkoutForm?.addEventListener('submit', event => {
        if (!validateCheckoutForm(checkoutForm)) {
            event.preventDefault();
            triggerToast('Vui lòng kiểm tra thông tin giao hàng.', 'error');
        }
    });

    // Cart quantity update handlers (AJAX)
    setupCartUpdateHandlers();

    const message = document.body.dataset.cartMessage;
    const error = document.body.dataset.cartError;
    if (message) triggerToast(message, 'success');
    if (error) triggerToast(error, 'error');
});

function setupCartUpdateHandlers() {
    // Quantity increase/decrease buttons
    document.querySelectorAll('.cart-qty-increase, .cart-qty-decrease').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const productId = btn.dataset.productId;
            const quantity = parseInt(btn.dataset.quantity);
            
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            try {
                const response = await fetch('/Cart/Update', {
                    method: 'POST',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: new URLSearchParams({
                        productId: productId,
                        quantity: quantity,
                        __RequestVerificationToken: token
                    })
                });

                if (response.ok) {
                    const data = await response.json();
                    if (data.success) {
                        updateCartDisplay(data);
                        triggerToast('Đã cập nhật giỏ hàng', 'success');
                    }
                } else {
                    triggerToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
                }
            } catch (error) {
                console.error('Cart update failed:', error);
                triggerToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
            }
        });
    });

    // Remove item buttons
    document.querySelectorAll('.cart-remove-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const productId = btn.dataset.productId;
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            try {
                const response = await fetch('/Cart/Remove', {
                    method: 'POST',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: new URLSearchParams({
                        productId: productId,
                        __RequestVerificationToken: token
                    })
                });

                if (response.ok) {
                    const data = await response.json();
                    if (data.success) {
                        updateCartDisplay(data);
                        triggerToast('Đã xóa khỏi giỏ hàng', 'success');
                    }
                } else {
                    triggerToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
                }
            } catch (error) {
                console.error('Cart remove failed:', error);
                triggerToast('Có lỗi xảy ra, vui lòng thử lại', 'error');
            }
        });
    });
}

function updateCartDisplay(cartData) {
    if (!cartData || !cartData.items) return;

    // Update quantity display for each item
    cartData.items.forEach(item => {
        const qtyDisplay = document.querySelector(`.cart-item[data-product-id="${item.productId}"] .cart-qty-display`);
        if (qtyDisplay) {
            qtyDisplay.textContent = item.quantity;
        }
        
        // Update quantity buttons data
        const increaseBtn = document.querySelector(`.cart-item[data-product-id="${item.productId}"] .cart-qty-increase`);
        const decreaseBtn = document.querySelector(`.cart-item[data-product-id="${item.productId}"] .cart-qty-decrease`);
        if (increaseBtn) increaseBtn.dataset.quantity = item.quantity + 1;
        if (decreaseBtn) decreaseBtn.dataset.quantity = item.quantity - 1;
    });

    // Remove items that no longer exist (quantity = 0)
    document.querySelectorAll('.cart-item').forEach(item => {
        const productId = parseInt(item.dataset.productId);
        if (!cartData.items.find(i => i.productId === productId)) {
            item.remove();
        }
    });

    // Update cart totals
    const totalPrice = document.getElementById('cart-total-price');
    if (totalPrice) {
        totalPrice.textContent = cartData.cartTotal.toLocaleString('vi-VN') + 'đ';
    }

    // Update checkout total display
    const checkoutTotalDisplay = document.getElementById('checkout-total-display');
    if (checkoutTotalDisplay) {
        checkoutTotalDisplay.textContent = cartData.cartTotal.toLocaleString('vi-VN') + 'đ';
        checkoutTotalDisplay.setAttribute('data-cart-total', cartData.cartTotal);
    }

    // Update QR amount if visible
    const qrAmount = document.getElementById('qr-amount');
    if (qrAmount && qrAmount.offsetParent !== null) {
        qrAmount.textContent = cartData.cartTotal.toLocaleString('vi-VN') + 'đ';
    }

    // Update cart counter badge
    const totalQuantity = cartData.items.length;
    const cartCounter = document.getElementById('cart-counter');
    if (cartCounter) {
        cartCounter.textContent = totalQuantity;
    }

    // Update checkout button state
    const checkoutBtn = document.getElementById('cart-checkout-btn');
    if (checkoutBtn) {
        checkoutBtn.disabled = cartData.items.length === 0;
    }

    // Show empty cart message if needed
    const cartItemsList = document.getElementById('cart-items-wrap');
    if (cartItemsList && cartData.items.length === 0) {
        cartItemsList.innerHTML = `
            <div class="cart-empty">
                <i class="bi bi-bag"></i>
                <strong>Giỏ hàng trống</strong>
                <span>Chọn món bánh bạn thích để bắt đầu đặt hàng.</span>
            </div>
        `;
        if (checkoutBtn) checkoutBtn.disabled = true;
    }
}

function closeCheckoutModal() {
    document.getElementById('checkout-modal-overlay')?.classList.remove('open');
    clearFormErrors(document.getElementById('checkout-form'));
}

function selectPaymentMethod(element) {
    document.querySelectorAll('.pay-opt-card').forEach(card => card.classList.remove('active'));
    element.classList.add('active');

    const paymentInput = document.getElementById('payment-method');
    const method = element.getAttribute('data-method') || 'COD';
    if (paymentInput) paymentInput.value = method;

    // Show/hide QR code and bank transfer section
    const qrSection = document.getElementById('qr-code-section');
    const bankTransferSection = document.getElementById('bank-transfer-section');
    const qrAmount = document.getElementById('qr-amount');
    const transferCode = document.getElementById('transfer-code');
    
    if (method === 'Bank') {
        if (qrSection) qrSection.style.display = 'block';
        if (bankTransferSection) bankTransferSection.style.display = 'block';
        
        // Generate and update bank transfer code
        const totalDisplay = document.getElementById('checkout-total-display');
        if (totalDisplay && qrAmount) {
            qrAmount.textContent = totalDisplay.textContent;
        }
        
        if (transferCode && totalDisplay) {
            const amount = parseInt(totalDisplay.textContent.replace(/\D/g, ''));
            transferCode.textContent = generateBankTransferCode(amount);
        }
    } else {
        if (qrSection) qrSection.style.display = 'none';
        if (bankTransferSection) bankTransferSection.style.display = 'none';
    }
}

function generateBankTransferCode(amount) {
    // Format: OISHI + date + time + random + amount
    // Example: OISHI2401202514050012500000
    const now = new Date();
    const dateStr = now.toISOString().slice(0, 10).replace(/-/g, '').slice(2); // DDMMYY
    const timeStr = String(now.getHours()).padStart(2, '0') + 
                    String(now.getMinutes()).padStart(2, '0') + 
                    String(now.getSeconds()).padStart(2, '0');
    const random = String(Math.floor(Math.random() * 1000)).padStart(3, '0');
    const amountStr = String(amount).padStart(10, '0');
    
    return `OISHI${dateStr}${timeStr}${random}${amountStr}`;
}

function copyTransferCode() {
    const transferCode = document.getElementById('transfer-code');
    if (!transferCode || !transferCode.value) return;
    
    navigator.clipboard.writeText(transferCode.value).then(() => {
        triggerToast('Đã sao chép mã chuyển khoản', 'success');
    }).catch(() => {
        // Fallback for older browsers
        transferCode.select();
        document.execCommand('copy');
        triggerToast('Đã sao chép mã chuyển khoản', 'success');
    });
}

function validateCheckoutForm(form) {
    let isValid = true;
    const nameInput = document.getElementById('customer-name');
    const phoneInput = document.getElementById('customer-phone');
    const addressInput = document.getElementById('customer-address');

    if (!nameInput?.value.trim()) {
        showError(nameInput, 'Vui lòng nhập họ tên.');
        isValid = false;
    }

    if (!phoneInput?.value.trim() || !/^(0[3|5|7|8|9])+([0-9]{8})$/.test(phoneInput.value.trim())) {
        showError(phoneInput, 'Số điện thoại không hợp lệ.');
        isValid = false;
    }

    if (!addressInput?.value.trim()) {
        showError(addressInput, 'Vui lòng nhập địa chỉ nhận bánh.');
        isValid = false;
    }

    return isValid;
}

function showError(inputEl, message) {
    if (!inputEl) return;

    const parent = inputEl.closest('.form-group');
    if (!parent) return;

    parent.querySelector('.input-error-msg')?.remove();
    inputEl.classList.add('invalid');

    const errSpan = document.createElement('span');
    errSpan.className = 'input-error-msg';
    errSpan.innerHTML = `<i class="bi bi-exclamation-circle"></i> ${message}`;
    parent.appendChild(errSpan);
}

function clearError(inputEl) {
    if (!inputEl) return;
    inputEl.classList.remove('invalid');
    inputEl.closest('.form-group')?.querySelector('.input-error-msg')?.remove();
}

function clearFormErrors(formContainer) {
    formContainer?.querySelectorAll('.form-control').forEach(input => clearError(input));
}

document.addEventListener('input', event => {
    if (event.target.classList.contains('form-control')) clearError(event.target);
});

function triggerToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container || !message) return;

    const toast = document.createElement('div');
    toast.className = `gorgeous-toast toast-${type}`;

    const iconClass = type === 'error'
        ? 'bi bi-exclamation-triangle'
        : type === 'info'
            ? 'bi bi-info-circle'
            : 'bi bi-check-circle';

    const titleText = type === 'error' ? 'Có lỗi' : type === 'info' ? 'Thông báo' : 'Thành công';

    toast.innerHTML = `
        <div class="toast-icon-wrap"><i class="${iconClass}"></i></div>
        <div class="toast-content-wrap">
            <span class="toast-title-text">${titleText}</span>
            <span class="toast-message-text">${message}</span>
        </div>
        <button class="toast-close-btn" onclick="dismissToast(this.parentElement)"><i class="bi bi-x-lg"></i></button>
        <div class="toast-progress-bar"><div class="toast-progress-fill"></div></div>
    `;

    container.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 50);

    const progressFill = toast.querySelector('.toast-progress-fill');
    if (progressFill) {
        progressFill.style.transition = 'transform 4000ms linear';
        setTimeout(() => { progressFill.style.transform = 'scaleX(0)'; }, 100);
    }

    toast.dataset.timerId = setTimeout(() => dismissToast(toast), 4000);
}

function dismissToast(toastElement) {
    if (!toastElement) return;
    if (toastElement.dataset.timerId) clearTimeout(parseInt(toastElement.dataset.timerId));
    toastElement.classList.add('hide');
    setTimeout(() => toastElement.remove(), 400);
}

function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);
    const icon = button?.querySelector('i');
    if (!input) return;

    const showPassword = input.type === 'password';
    input.type = showPassword ? 'text' : 'password';

    icon?.classList.toggle('fa-eye', !showPassword);
    icon?.classList.toggle('fa-eye-slash', showPassword);
    icon?.classList.toggle('bi-eye', !showPassword);
    icon?.classList.toggle('bi-eye-slash', showPassword);
}

function switchAuthPanel(panelName) {
    const panels = {
        login: document.getElementById('login-panel'),
        register: document.getElementById('register-panel'),
        forgot: document.getElementById('forgot-panel')
    };

    Object.values(panels).forEach(panel => panel?.classList.add('hidden'));
    panels[panelName]?.classList.remove('hidden');
}

function checkPasswordStrength(password) {
    const bar = document.getElementById('strength-bar');
    const label = document.getElementById('strength-label');
    if (!bar || !label) return;

    let score = 0;
    if (password.length >= 6) score++;
    if (password.length >= 10) score++;
    if (/[A-Z]/.test(password) && /[a-z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[^A-Za-z0-9]/.test(password)) score++;

    const states = [
        { text: 'Rat yeu', width: '20%', color: '#dc3545' },
        { text: 'Yeu', width: '35%', color: '#fd7e14' },
        { text: 'Vua', width: '55%', color: '#ffc107' },
        { text: 'Tot', width: '75%', color: '#20c997' },
        { text: 'Manh', width: '100%', color: '#198754' }
    ];
    const state = states[Math.min(Math.max(score - 1, 0), states.length - 1)];

    bar.style.width = password ? state.width : '0';
    bar.style.backgroundColor = state.color;
    label.textContent = password ? state.text : 'Rat yeu';
}

function triggerSocialLogin(provider) {
    triggerToast(`${provider} chua duoc cau hinh. Vui long dang nhap bang email.`, 'info');
}

function handleForgotPasswordRequest(event) {
    event.preventDefault();
    triggerToast('Vui long dung trang Quen mat khau de dat lai mat khau.', 'info');
    window.location.href = '/Account/ForgotPassword';
}

function triggerBuyNow(productId, price) {
    const buyNowInput = document.getElementById('buy-now-product-id');
    if (buyNowInput) buyNowInput.value = productId;
    
    const checkoutTotalDisplay = document.getElementById('checkout-total-display');
    if (checkoutTotalDisplay) {
        checkoutTotalDisplay.textContent = price.toLocaleString('vi-VN') + 'đ';
    }
    
    document.getElementById('checkout-modal-overlay')?.classList.add('open');
}
