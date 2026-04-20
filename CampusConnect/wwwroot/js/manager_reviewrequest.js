//  manager_reviewrequests.js


document.addEventListener('DOMContentLoaded', function () {

    var ROWS_PER_PAGE = 10;
    var rows = Array.from(document.querySelectorAll('#review-table tbody tr.review-row'));
    var paginationBar = document.getElementById('pagination-review');

    // Nothing to paginate
    if (!paginationBar || rows.length <= ROWS_PER_PAGE) return;

    var currentPage = 1;
    var totalPages = Math.ceil(rows.length / ROWS_PER_PAGE);

    function showPage(page) {
        currentPage = page;
        var start = (page - 1) * ROWS_PER_PAGE;
        var end = start + ROWS_PER_PAGE;

        rows.forEach(function (row, index) {
            row.style.display = (index >= start && index < end) ? '' : 'none';
        });

        renderPagination();
    }

    function renderPagination() {
        paginationBar.innerHTML = '';

        // Prev button
        var prev = document.createElement('button');
        prev.textContent = '← Prev';
        prev.className = 'page-btn';
        prev.disabled = currentPage === 1;
        prev.addEventListener('click', function () {
            if (currentPage > 1) showPage(currentPage - 1);
        });
        paginationBar.appendChild(prev);

        // Page number buttons
        for (var i = 1; i <= totalPages; i++) {
            (function (page) {
                var btn = document.createElement('button');
                btn.textContent = page;
                btn.className = 'page-btn' + (page === currentPage ? ' active' : '');
                btn.addEventListener('click', function () { showPage(page); });
                paginationBar.appendChild(btn);
            })(i);
        }

        // Next button
        var next = document.createElement('button');
        next.textContent = 'Next →';
        next.className = 'page-btn';
        next.disabled = currentPage === totalPages;
        next.addEventListener('click', function () {
            if (currentPage < totalPages) showPage(currentPage + 1);
        });
        paginationBar.appendChild(next);

        // Page info
        var info = document.createElement('span');
        info.className = 'page-info';
        var start = (currentPage - 1) * ROWS_PER_PAGE + 1;
        var end = Math.min(currentPage * ROWS_PER_PAGE, rows.length);
        info.textContent = 'Showing ' + start + '–' + end + ' of ' + rows.length;
        paginationBar.appendChild(info);
    }

    showPage(1);

});