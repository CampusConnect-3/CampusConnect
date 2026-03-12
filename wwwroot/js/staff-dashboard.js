// Global variable to store the dragged element
let draggedElement = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Staff Dashboard initialized');
    initializeDragAndDrop();
    initializeViewDetailsButtons();
});

function initializeDragAndDrop() {
    // Add dragstart event to all request cards
    document.querySelectorAll('.request-card').forEach(card => {
        card.addEventListener('dragstart', function(e) {
            draggedElement = this;
            this.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/html', this.innerHTML);
            console.log('Drag started for request:', this.getAttribute('data-request-id'));
        });

        card.addEventListener('dragend', function(e) {
            this.classList.remove('dragging');
            draggedElement = null;
        });
    });

    // Add drop zone events to all columns
    document.querySelectorAll('.drop-zone').forEach(zone => {
        zone.addEventListener('dragover', function(e) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            this.classList.add('drag-over');
        });

        zone.addEventListener('dragleave', function(e) {
            // Only remove if we're leaving the drop zone itself, not a child
            if (e.target === this) {
                this.classList.remove('drag-over');
            }
        });

        zone.addEventListener('drop', function(e) {
            e.preventDefault();
            this.classList.remove('drag-over');
            
            if (draggedElement) {
                const requestId = draggedElement.getAttribute('data-request-id');
                const newStatus = this.getAttribute('data-status');
                updateRequestStatus(requestId, newStatus);
            }
        });
    });
}

function initializeViewDetailsButtons() {
    document.querySelectorAll('.view-details-btn').forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.stopPropagation();
            e.preventDefault();
            const requestId = this.getAttribute('data-request-id');
            console.log('View details clicked for request:', requestId);
            showRequestDetail(requestId);
        });
    });
}

function updateRequestStatus(requestId, newStatus) {
    console.log('Updating request', requestId, 'to status', newStatus);

    // Get the antiforgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (!token) {
        console.error('Antiforgery token not found');
        alert('Security token not found. Please refresh the page.');
        return;
    }

    // Show loading indicator
    const originalCursor = document.body.style.cursor;
    document.body.style.cursor = 'wait';

    // Update status via AJAX
    fetch('/StaffPages/Dashboard?handler=UpdateStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({
            requestId: parseInt(requestId),
            newStatus: newStatus
        })
    })
    .then(response => {
        console.log('Response status:', response.status);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        console.log('Response data:', data);
        document.body.style.cursor = originalCursor;
        
        if (data.success) {
            // Reload page to show updated status
            location.reload();
        } else {
            alert('Failed to update status: ' + (data.message || 'Unknown error'));
        }
    })
    .catch(error => {
        document.body.style.cursor = originalCursor;
        console.error('Error:', error);
        alert('Error updating request status. Please try again.');
    });
}

function showRequestDetail(requestId) {
    console.log('Loading details for request:', requestId);
    
    // Show loading in modal
    const modalContent = document.getElementById('modalContent');
    modalContent.innerHTML = '<div class="text-center p-4"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';
    
    // Show modal immediately
    const modal = new bootstrap.Modal(document.getElementById('requestDetailModal'));
    modal.show();
    
    // Load request detail via AJAX
    fetch(`/StaffPages/RequestDetail?requestId=${requestId}`)
        .then(response => {
            console.log('Detail response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.text();
        })
        .then(html => {
            console.log('Detail HTML received, length:', html.length);
            modalContent.innerHTML = html;
        })
        .catch(error => {
            console.error('Error loading details:', error);
            modalContent.innerHTML = `
                <div class="alert alert-danger">
                    <h5>Error Loading Request Details</h5>
                    <p>${error.message}</p>
                    <p>Request ID: ${requestId}</p>
                </div>
            `;
        });
}

// Prevent default drag behavior on the entire document
document.addEventListener('dragover', function(e) {
    e.preventDefault();
}, false);

document.addEventListener('drop', function(e) {
    e.preventDefault();
}, false);