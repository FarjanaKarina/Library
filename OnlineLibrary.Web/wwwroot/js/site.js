// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function openSearchBar() {
    const searchInput = document.getElementById("searchInput");

    if (!searchInput) return;

    // Scroll to search bar smoothly
    searchInput.scrollIntoView({ behavior: "smooth", block: "center" });

    // Focus input
    setTimeout(() => {
        searchInput.focus();
    }, 400);
}


window.addEventListener("scroll", function () {
    const icon = document.getElementById("navSearchIcon");
    if (!icon) return;

    // Only for guests
    if (document.body.classList.contains("page-landing")) {
        if (window.scrollY > 200) {
            icon.classList.remove("d-none");
        } else {
            icon.classList.add("d-none");
        }
    }
});
