// Global variable to store the dragged element
let draggedElement = null;

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}

function init() {
    console.log('Staff Dashboard initializing...');
    console.log('document.readyState:', document.readyState);

    // Wait a bit for everything to be ready
    setTimeout(() => {
        initializeDragAndDrop();
        initializeViewDetailsButtons();
    }, 100);
}

function initializeDragAndDrop() {
    // Add dragstart event to all request cards
    const cards = document.querySelectorAll('.request-card');
    console.log('Initializing drag and drop...');
    console.log('Found request cards:', cards.length);

    if (cards.length === 0) {
        console.warn('No request cards found! Check if elements exist in DOM.');
        return;
    }

    cards.forEach((card, index) => {
        console.log(`Setting up card ${index + 1}:`, card.getAttribute('data-request-id'));

        // Ensure draggable attribute is set
        card.draggable = true;

        // Remove any existing event listeners by cloning (clean slate)
        const newCard = card.cloneNode(true);
        card.parentNode.replaceChild(newCard, card);

        // Add dragstart event
        newCard.ondragstart = function (e) {
            console.log('🎯 DRAGSTART event fired!');
            draggedElement = this;
            this.classList.add('dragging');
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', this.getAttribute('data-request-id'));
            console.log('Dragging request:', this.getAttribute('data-request-id'));
        };

        // Add dragend event
        newCard.ondragend = function (e) {
            console.log('🏁 DRAGEND event fired');
            this.classList.remove('dragging');

            // Remove drag-over class from all zones
            document.querySelectorAll('.drop-zone').forEach(zone => {
                zone.classList.remove('drag-over');
            });

            draggedElement = null;
        };

        // Re-add click handler for view details button
        const btn = newCard.querySelector('.view-details-btn');
        if (btn) {
            btn.onclick = function (e) {
                e.stopPropagation();
                e.preventDefault();
                const requestId = this.getAttribute('data-request-id');
                console.log('View details clicked for request:', requestId);
                showRequestDetail(requestId);
            };
        }
    });

    // Setup drop zones
    const zones = document.querySelectorAll('.drop-zone');
    console.log('Found drop zones:', zones.length);

    zones.forEach((zone, index) => {
        console.log(`Setting up drop zone ${index + 1}:`, zone.getAttribute('data-status'));

        zone.ondragover = function (e) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            this.classList.add('drag-over');
            return false;
        };

        zone.ondragenter = function (e) {
            e.preventDefault();
            this.classList.add('drag-over');
        };

        zone.ondragleave = function (e) {
            // Check if we're leaving the drop zone itself
            const rect = this.getBoundingClientRect();
            if (e.clientX < rect.left || e.clientX >= rect.right ||
                e.clientY < rect.top || e.clientY >= rect.bottom) {
                this.classList.remove('drag-over');
            }
        };

        zone.ondrop = function (e) {
            e.preventDefault();
            e.stopPropagation();

            console.log('💧 DROP event fired!');
            this.classList.remove('drag-over');

            if (draggedElement) {
                const requestId = draggedElement.getAttribute('data-request-id');
                const oldStatus = draggedElement.closest('.drop-zone').getAttribute('data-status');
                const newStatus = this.getAttribute('data-status');

                console.log('Dropped request', requestId, 'from', oldStatus, 'to', newStatus);

                if (oldStatus !== newStatus) {
                    // Move the card visually first (optimistic update)
                    this.appendChild(draggedElement);

                    // Update badge counts
                    updateBadgeCounts();

                    // Then update on server
                    updateRequestStatus(requestId, newStatus);
                } else {
                    console.log('No status change needed');
                }
            } else {
                console.warn('No dragged element found!');
            }

            return false;
        };
    });

    console.log('✅ Drag and drop initialization complete');
}

function updateBadgeCounts() {
    document.querySelectorAll('.drop-zone').forEach(zone => {
        const badge = zone.closest('.card').querySelector('.badge');
        const count = zone.querySelectorAll('.request-card').length;
        if (badge) {
            badge.textContent = count;
        }
    });
}

function initializeViewDetailsButtons() {
    const buttons = document.querySelectorAll('.view-details-btn');
    console.log('Found view details buttons:', buttons.length);

    buttons.forEach(btn => {
        // Use onclick to ensure it works
        btn.onclick = function (e) {
            e.stopPropagation();
            e.preventDefault();
            const requestId = this.getAttribute('data-request-id');
            console.log('👁️ View details clicked for request:', requestId);
            showRequestDetail(requestId);
        };
    });
}

function updateRequestStatus(requestId, newStatus) {
    console.log('📤 Updating request', requestId, 'to status', newStatus);

    // Get the antiforgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (!token) {
        console.error('❌ Antiforgery token not found');
        alert('Security token not found. Please refresh the page.');
        return;
    }

    console.log('🔑 Token found:', token.substring(0, 20) + '...');

    // Show loading indicator
    const originalCursor = document.body.style.cursor;
    document.body.style.cursor = 'wait';

    // Update status via AJAX
    fetch('/StaffPages/Dashboard?handler=UpdateStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token,  // CHANGED: Use X-CSRF-TOKEN header
            'RequestVerificationToken': token  // Keep this too for compatibility
        },
        body: JSON.stringify({
            requestId: parseInt(requestId),
            newStatus: newStatus
        })
    })
        .then(response => {
            console.log('📥 Response status:', response.status);
            console.log('📥 Response headers:', [...response.headers.entries()]);
            
            // Try to get response text first to see what the server is returning
            return response.text().then(text => {
                console.log('📥 Response text:', text);
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}, body: ${text}`);
                }
                
                try {
                    return JSON.parse(text);
                } catch (e) {
                    console.error('❌ Failed to parse JSON:', e);
                    throw new Error('Invalid JSON response: ' + text);
                }
            });
        })
        .then(data => {
            console.log('📥 Response data:', data);
            document.body.style.cursor = originalCursor;

            if (data.success) {
                console.log('✅ Status updated successfully');
                // Show success notification
                showNotification('Status updated successfully!', 'success');
            } else {
                console.error('❌ Server returned failure:', data.message);
                alert('Failed to update status: ' + (data.message || 'Unknown error'));
                // Reload to revert the optimistic update
                location.reload();
            }
        })
        .catch(error => {
            document.body.style.cursor = originalCursor;
            console.error('❌ Error updating status:', error);
            alert('Error updating request status. Please try again.\n\nDetails: ' + error.message);
            // Reload to revert the optimistic update
            location.reload();
        });
}

function showRequestDetail(requestId) {
    console.log('📄 Loading details for request:', requestId);

    // Show loading in modal
    const modalContent = document.getElementById('modalContent');
    if (!modalContent) {
        console.error('❌ Modal content element not found');
        return;
    }

    modalContent.innerHTML = '<div class="text-center p-4"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';

    // Show modal immediately
    const modalElement = document.getElementById('requestDetailModal');
    if (!modalElement) {
        console.error('❌ Modal element not found');
        return;
    }

    const modal = new bootstrap.Modal(modalElement);
    modal.show();

    // Load content via AJAX - ADD modal=true parameter
    fetch(`/RequestPages/Details?id=${requestId}&modal=true`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.text();
        })
        .then(html => {
            modalContent.innerHTML = html;
        })
        .catch(error => {
            console.error('❌ Error loading details:', error);
            modalContent.innerHTML = '<div class="alert alert-danger">Failed to load request details.</div>';
        });
}

function showNotification(message, type = 'info') {
    // Simple notification - you can enhance this
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
    notification.style.zIndex = '9999';
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 3000);
}
