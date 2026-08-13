// Google Login Handler cho Oisipan Bakery MVC
window.GoogleLoginHandler = (function () {
    'use strict';

    function initialize(clientId) {
        if (!clientId) {
            console.error('Google Client ID chưa được cấu hình.');
            return;
        }

        if (typeof google === 'undefined' || !google.accounts) {
            console.error('Google Identity Services SDK chưa load.');
            return;
        }

        google.accounts.id.initialize({
            client_id: clientId,
            callback: handleCredentialResponse,
            auto_select: false,
            cancel_on_tap_outside: true
        });

        var wrapper = document.getElementById('google-signin-button-wrapper');
        var buttonDiv = document.getElementById('google-signin-button');
        if (buttonDiv && wrapper) {
            google.accounts.id.renderButton(buttonDiv, {
                theme: 'outline',
                size: 'large',
                type: 'standard',
                shape: 'rectangular',
                text: 'signin_with',
                logo_alignment: 'left'
            });
        }
    }

    function handleCredentialResponse(response) {
        showLoading(true);
        clearMessages();

        fetch('/Account/GoogleLoginCallback', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'same-origin',
            body: JSON.stringify({ idToken: response.credential })
        })
            .then(function (res) {
                return res.text().then(function (text) {
                    try {
                        var data = JSON.parse(text);
                        return { ok: res.ok, data: data };
                    } catch (e) {
                        console.error('Non-JSON response:', text);
                        throw new Error('Lỗi máy chủ (Vui lòng kiểm tra Console log).');
                    }
                });
            })
            .then(function (result) {
                if (!result.ok || !result.data.success) {
                    throw new Error((result.data && result.data.message) || 'Đăng nhập Google thất bại.');
                }

                showMessage('Đăng nhập thành công! Đang chuyển hướng...', 'success');
                setTimeout(function () {
                    window.location.href = result.data.redirectUrl || '/';
                }, 800);
            })
            .catch(function (error) {
                console.error('Google login error:', error);
                showMessage(error.message || 'Có lỗi xảy ra khi đăng nhập bằng Google.', 'danger');
            })
            .finally(function () {
                showLoading(false);
            });
    }

    function showLoading(show) {
        var overlay = document.getElementById('loading-overlay');
        if (overlay) {
            overlay.style.display = show ? 'flex' : 'none';
        }
    }

    function clearMessages() {
        var existing = document.getElementById('google-login-alert');
        if (existing) {
            existing.remove();
        }
    }

    function showMessage(message, type) {
        clearMessages();

        var alertDiv = document.createElement('div');
        alertDiv.id = 'google-login-alert';
        alertDiv.className = 'alert alert-' + type + ' mt-3';
        alertDiv.textContent = message;

        var container = document.getElementById('google-signin-button-wrapper');
        if (container && container.parentElement) {
            container.parentElement.insertAdjacentElement('afterend', alertDiv);
        }
    }

    return {
        initialize: initialize
    };
})();
