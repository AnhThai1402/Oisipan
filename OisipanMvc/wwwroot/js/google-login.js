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

        var buttonDiv = document.getElementById('google-signin-button');
        if (buttonDiv) {
            google.accounts.id.renderButton(buttonDiv, {
                theme: 'outline',
                size: 'large',
                width: buttonDiv.offsetWidth || 300,
                text: 'signin_with',
                shape: 'rectangular',
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
            body: JSON.stringify({ idToken: response.credential })
        })
            .then(function (res) {
                return res.json().then(function (data) {
                    return { ok: res.ok, data: data };
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

        var container = document.querySelector('.google-signin-container');
        if (container) {
            container.insertAdjacentElement('afterend', alertDiv);
        }
    }

    return {
        initialize: initialize
    };
})();
