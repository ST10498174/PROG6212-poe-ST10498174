// RaceDay front-end behaviour. Kept deliberately small: the server renders every page,
// and these helpers only improve the experience (nothing depends on JavaScript to work).
(function () {
    "use strict";

    // Organiser sidebar on small screens.
    const sidebar = document.getElementById("rcSidebar");
    const toggle = document.getElementById("rcToggle");
    const backdrop = document.getElementById("rcBackdrop");
    if (sidebar && toggle && backdrop) {
        const setOpen = (open) => {
            sidebar.classList.toggle("show", open);
            backdrop.classList.toggle("show", open);
            toggle.setAttribute("aria-expanded", String(open));
        };
        toggle.addEventListener("click", () => setOpen(!sidebar.classList.contains("show")));
        backdrop.addEventListener("click", () => setOpen(false));
    }

    // Forms or buttons marked data-confirm ask before a destructive action (delete, withdraw).
    document.addEventListener("submit", (event) => {
        const form = event.target;
        const message = form.getAttribute("data-confirm") || event.submitter?.getAttribute("data-confirm");
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
    });

    // Prevent double submissions: disable the clicked submit button once the form is valid and sending.
    document.addEventListener("submit", (event) => {
        if (event.defaultPrevented) return;
        const form = event.target;
        if (window.jQuery && window.jQuery(form).data("validator") && !window.jQuery(form).valid()) return;
        const button = event.submitter;
        if (button && !button.hasAttribute("data-no-busy")) {
            setTimeout(() => {
                button.disabled = true;
                button.dataset.label = button.innerHTML;
                button.innerHTML = '<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>' + (button.dataset.busy || "Saving…");
            }, 0);
        }
    });

    // Live preview for image inputs (banner and profile picture) before uploading.
    document.querySelectorAll("input[type=file][data-preview]").forEach((input) => {
        input.addEventListener("change", () => {
            const target = document.getElementById(input.dataset.preview);
            const file = input.files && input.files[0];
            if (!target || !file || !file.type.startsWith("image/")) return;
            if (file.size > 5 * 1024 * 1024) {
                alert("That image is larger than 5 MB. Please choose a smaller file.");
                input.value = "";
                return;
            }
            target.src = URL.createObjectURL(file);
        });
    });

    // Category form: show age fields or distance field depending on the chosen type.
    document.querySelectorAll("[data-category-type]").forEach((select) => {
        const scope = select.closest("form");
        const apply = () => {
            const isAge = select.value === "Age";
            scope.querySelectorAll("[data-for=Age]").forEach((el) => el.hidden = !isAge);
            scope.querySelectorAll("[data-for=Distance]").forEach((el) => el.hidden = isAge);
        };
        select.addEventListener("change", apply);
        apply();
    });
})();
