document.addEventListener("DOMContentLoaded", () => {
    const toggleBtn = document.getElementById("btnToggleGestion") || document.querySelector('.sidebar-toggle[href="#submenuInicio"]');
    const submenuEl = document.getElementById("submenuInicio");
    const bs = window["bootstrap"];

    if (toggleBtn && submenuEl && bs && bs.Collapse) {
        const collapseInstance = bs.Collapse.getOrCreateInstance(submenuEl, {
            toggle: false
        });

        toggleBtn.addEventListener("click", (e) => {
            e.preventDefault();
            e.stopPropagation();
            collapseInstance.toggle();
            toggleBtn.classList.toggle("open");
            toggleBtn.setAttribute(
                "aria-expanded",
                submenuEl.classList.contains("show") ? "false" : "true"
            );
        });
    }

    // Asegura que los enlaces del submenú naveguen directamente sin ser interceptados
    document.querySelectorAll(".sidebar-submenu a.nav-link").forEach((link) => {
        link.addEventListener("click", (e) => {
            const destino = link.getAttribute("href");
            if (destino && destino !== "#") {
                e.stopPropagation();
                window.location.href = destino;
            }
        });
    });
});