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
/* For Wishlist */
function toggleWishlist(bookId, btn) {
    fetch('/Wishlist/Toggle', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: 'bookId=' + bookId
    })
        .then(r => {
            if (r.status === 401) {
                window.location.href = '/Account/Login';
                return;
            }
            return r.json();
        })
        .then(data => {
            if (!data) return;

            btn.classList.toggle('active', data.added);
        });
}
/* For Membership */
function openMembershipModal() {
    fetch('/Student/ApplyMembership')
        .then(r => r.text())
        .then(html => {
            document.getElementById('authModalContent').innerHTML = html;
            new bootstrap.Modal(document.getElementById('authModal')).show();
        });
}
function updateAmount() {
    const plan = document.getElementById("planSelect");
    const amount = plan.options[plan.selectedIndex].dataset.amount;
    document.getElementById("amount").value = amount || "";
}
