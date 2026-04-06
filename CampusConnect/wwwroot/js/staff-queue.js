
console.log('staff-queue.js loaded successfully');

function showRequestDetail(requestId) {
    console.log('Loading request details for ID:', requestId);
    
    // Show loading state
    const modalContent = document.getElementById('modalContent');
    if (!modalContent) {
        console.error('Modal content element not found!');
        alert('Error: Modal content element not found');
        return;
    }
    
    modalContent.innerHTML = '<div class="text-center p-5"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';
    
    // Show modal immediately with loading state
    const modalElement = document.getElementById('requestDetailModal');
    if (!modalElement) {
        console.error('Modal element not found!');
        alert('Error: Modal element not found');
        return;
    }
    
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
    
    // Load request detail via AJAX
    fetch(`/StaffPages/RequestDetail?requestId=${requestId}`)
        .then(response => {
            console.log('Response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.text();
        })
        .then(html => {
            console.log('Received HTML, updating modal');
            modalContent.innerHTML = html;
        })
        .catch(error => {
            console.error('Error loading request details:', error);
            modalContent.innerHTML = `
                <div class="alert alert-danger">
                    <i class="bi bi-exclamation-triangle"></i>
                    Failed to load request details. Please try again.
                    <br><small>Error: ${error.message}</small>
                </div>
            `;
        });
}
