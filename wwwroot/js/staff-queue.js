function showRequestDetail(requestId) {
    // Load request detail via AJAX
    fetch(`/StaffPages/RequestDetail?requestId=${requestId}`)
        .then(response => response.text())
        .then(html => {
            document.getElementById('modalContent').innerHTML = html;
            const modal = new bootstrap.Modal(document.getElementById('requestDetailModal'));
            modal.show();
        })
        .catch(error => console.error('Error:', error));
}