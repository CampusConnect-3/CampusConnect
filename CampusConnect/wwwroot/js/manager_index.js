//  manager_index.js


document.addEventListener('DOMContentLoaded', function () {

    const ROWS_PER_PAGE = 10;
    const rows = Array.from(document.querySelectorAll('.request-row'));
    let currentPage = 1;
    const totalPages = Math.ceil(rows.length / ROWS_PER_PAGE);

    // Click row to navigate
    rows.forEach(function (row) {
        row.addEventListener('click', function (e) {
            if (e.target.closest('a')) return;
            var id = row.getAttribute('data-id');
            if (id) window.location.href = '/Manager/ManageRequest?id=' + id;
        });
    });

    function showPage(page) {
        currentPage = page;
        const start = (page - 1) * ROWS_PER_PAGE;
        const end = start + ROWS_PER_PAGE;

        rows.forEach(function (row, index) {
            row.style.display = (index >= start && index < end) ? '' : 'none';
        });

        renderPagination();
    }

    function renderPagination() {
        const container = document.getElementById('pagination');
        container.innerHTML = '';

        // Previous button
        const prev = document.createElement('button');
        prev.textContent = '← Prev';
        prev.className = 'page-btn' + (currentPage === 1 ? ' disabled' : '');
        prev.disabled = currentPage === 1;
        prev.addEventListener('click', function () { if (currentPage > 1) showPage(currentPage - 1); });
        container.appendChild(prev);

        // Page number buttons
        for (let i = 1; i <= totalPages; i++) {
            const btn = document.createElement('button');
            btn.textContent = i;
            btn.className = 'page-btn' + (i === currentPage ? ' active' : '');
            btn.addEventListener('click', (function (page) {
                return function () { showPage(page); };
            })(i));
            container.appendChild(btn);
        }

        // Next button
        const next = document.createElement('button');
        next.textContent = 'Next →';
        next.className = 'page-btn' + (currentPage === totalPages ? ' disabled' : '');
        next.disabled = currentPage === totalPages;
        next.addEventListener('click', function () { if (currentPage < totalPages) showPage(currentPage + 1); });
        container.appendChild(next);

        // Page info
        const info = document.createElement('span');
        info.className = 'page-info';
        const start = (currentPage - 1) * ROWS_PER_PAGE + 1;
        const end = Math.min(currentPage * ROWS_PER_PAGE, rows.length);
        info.textContent = 'Showing ' + start + '–' + end + ' of ' + rows.length;
        container.appendChild(info);
    }

    if (rows.length > 0) {
        showPage(1);

        // Inject pagination container below the table card
        const tableCard = document.querySelector('.table-card');
        const paginationDiv = document.createElement('div');
        paginationDiv.id = 'pagination';
        paginationDiv.className = 'pagination-bar';
        tableCard.insertAdjacentElement('afterend', paginationDiv);
    }

});