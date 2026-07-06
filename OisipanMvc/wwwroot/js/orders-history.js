document.addEventListener('DOMContentLoaded', function () {
    const statusMap = {
        'chờ xác nhận': 1,
        'đã xác nhận': 2,
        'đang chuẩn bị': 3,
        'đang giao': 4,
        'đã giao': 5,
        'hoàn thành': 5,
        'đã hủy': -1
    };

    function normalize(s) {
        return (s || '').toString().trim().toLowerCase();
    }

    document.querySelectorAll('.shipping-progress-card').forEach(function (card) {
        const rawStatus = card.getAttribute('data-status') || '';
        const statusKey = normalize(rawStatus);
        const currentStep = statusMap[statusKey] ?? 1;
        const steps = card.querySelectorAll('.shipping-step');
        const label = card.querySelector('[data-progress-label]');
        const badge = card.querySelector('[data-progress-badge]');

        // clear canceled class by default then set if needed
        card.classList.remove('is-canceled');

        if (currentStep === -1) {
            card.classList.add('is-canceled');
            label.textContent = 'Đơn hàng đã bị hủy';
            badge.textContent = 'Đã hủy';
            // mark only the first step as active and leave others neutral
            steps.forEach(function (step, index) {
                step.classList.remove('is-active', 'is-done');
                if (index === 0) {
                    step.classList.add('is-active');
                }
            });
            return;
        }

        // for completed/đã giao statuses, mark all done and active last
        const isCompleted = currentStep >= steps.length;

        steps.forEach(function (step, index) {
            const stepNumber = index + 1;
            step.classList.remove('is-active', 'is-done');

            if (isCompleted) {
                step.classList.add('is-done');
                if (stepNumber === steps.length) {
                    step.classList.add('is-active');
                }
            } else {
                if (stepNumber < currentStep) {
                    step.classList.add('is-done');
                } else if (stepNumber === currentStep) {
                    step.classList.add('is-active');
                }
            }
        });

        // restore readable label/badge
        label.textContent = rawStatus || 'Đang xử lý';
        badge.textContent = rawStatus || '';
    });
});
