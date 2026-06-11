(() => {
    const sidebar = document.getElementById("admin-sidebar");
    const overlay = document.getElementById("admin-overlay");
    const sidebarToggle = document.getElementById("sidebar-toggle");

    const closeSidebar = () => {
        document.body.classList.remove("sidebar-open");
    };

    sidebarToggle?.addEventListener("click", () => {
        document.body.classList.toggle("sidebar-open");
    });

    overlay?.addEventListener("click", closeSidebar);
    sidebar?.querySelectorAll("a").forEach((link) => link.addEventListener("click", closeSidebar));

    document.getElementById("density-toggle")?.addEventListener("click", () => {
        document.body.classList.toggle("compact-density");
    });

    // Modal functionality
    document.querySelectorAll("[data-open-modal]").forEach((button) => {
        button.addEventListener("click", () => {
            const modalId = button.getAttribute("data-open-modal");
            const modal = document.getElementById(modalId);
            if (modal) {
                modal.classList.add("active");
                document.body.style.overflow = "hidden";
            }
        });
    });

    document.querySelectorAll("[data-close-modal]").forEach((button) => {
        button.addEventListener("click", () => {
            document.querySelectorAll(".modal.active").forEach((m) => m.classList.remove("active"));
            document.body.style.overflow = "";
        });
    });

    document.querySelectorAll(".modal").forEach((modal) => {
        modal.addEventListener("click", (e) => {
            if (e.target === modal) {
                modal.classList.remove("active");
                document.body.style.overflow = "";
            }
        });
    });

    // Filter functionality
    document.querySelectorAll("[data-filter]").forEach((button) => {
        button.addEventListener("click", () => {
            const group = button.closest(".segmented");
            const scope = button.closest("section")?.nextElementSibling?.matches("[data-filter-scope]")
                ? button.closest("section").nextElementSibling
                : document.querySelector("[data-filter-scope]");
            const filter = button.dataset.filter;

            group?.querySelectorAll("button").forEach((item) => item.classList.remove("active"));
            button.classList.add("active");

            scope?.querySelectorAll("[data-status]").forEach((item) => {
                item.hidden = filter !== "all" && item.dataset.status !== filter;
            });
        });
    });

    // Drawer functionality (legacy)
    document.querySelectorAll("[data-open-drawer]").forEach((button) => {
        button.addEventListener("click", () => {
            document.getElementById(button.dataset.openDrawer)?.classList.add("open");
        });
    });

    document.querySelectorAll("[data-close-drawer]").forEach((button) => {
        button.addEventListener("click", () => {
            button.closest(".admin-drawer")?.classList.remove("open");
        });
    });

    // Order row selection
    document.querySelectorAll(".order-row").forEach((row) => {
        row.addEventListener("click", () => {
            document.querySelectorAll(".order-row").forEach((item) => item.classList.remove("active"));
            row.classList.add("active");
        });
    });
})();
