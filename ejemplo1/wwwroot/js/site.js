document.addEventListener("DOMContentLoaded", () => {
    const toggleBtn = document.querySelector('.sidebar-toggle[href="#submenuInicio"]');
    const submenuEl = document.getElementById("submenuInicio");

    if (toggleBtn && submenuEl && window.bootstrap) {
        const collapseInstance = bootstrap.Collapse.getOrCreateInstance(submenuEl, {
            toggle: false
        });

        toggleBtn.addEventListener("click", (e) => {
            e.preventDefault();
            collapseInstance.toggle();
            toggleBtn.classList.toggle("open");
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