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
            console.log('📥 Response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
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
            alert('Error updating request status. Please try again.');
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

    // Load request detail via AJAX
    fetch(`/StaffPages/RequestDetail?requestId=${requestId}`)
        .then(response => {
            console.log('📥 Detail response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.text();
        })
        .then(html => {
            console.log('📄 Detail HTML received, length:', html.length);
            modalContent.innerHTML = html;
            initializeModalFormHandlers(requestId);
        })
        .catch(error => {
            console.error('❌ Error loading details:', error);
            modalContent.innerHTML = `
                <div class="alert alert-danger">
                    <h5>Error Loading Request Details</h5>
                    <p>${error.message}</p>
                    <p>Request ID: ${requestId}</p>
                </div>
            `;
        });
}

function initializeModalFormHandlers(requestId) {
    const modalContent = document.getElementById('modalContent');
    if (!modalContent) return;

    // Get the antiforgery token from the main form
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    // Handle attachment upload form
    const attachmentForm = modalContent.querySelector('form[asp-page-handler="AddAttachment"], form[action*="AddAttachment"]');
    if (attachmentForm) {
        attachmentForm.onsubmit = function (e) {
            e.preventDefault();
            console.log('📎 Attachment form submitted');

            const formData = new FormData(this);
            
            // Add the antiforgery token if not already in form
            if (token && !formData.has('__RequestVerificationToken')) {
                formData.append('__RequestVerificationToken', token);
            }

            fetch('/StaffPages/RequestDetail?handler=AddAttachment', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }
                    return response.text();
                })
                .then(html => {
                    console.log('✅ Attachment uploaded successfully');
                    // Update modal content with refreshed data
                    modalContent.innerHTML = html;
                    // Re-initialize handlers for the new content
                    initializeModalFormHandlers(requestId);
                    showNotification('Attachment uploaded successfully!', 'success');
                })
                .catch(error => {
                    console.error('❌ Error uploading attachment:', error);
                    alert('Error uploading attachment. Please try again.');
                });

            return false;
        };
    }

    // Handle comment form
    const commentForm = modalContent.querySelector('form[asp-page-handler="AddComment"], form[action*="AddComment"]');
    if (commentForm) {
        commentForm.onsubmit = function (e) {
            e.preventDefault();
            console.log('💬 Comment form submitted');

            const formData = new FormData(this);
            
            // Add the antiforgery token if not already in form
            if (token && !formData.has('__RequestVerificationToken')) {
                formData.append('__RequestVerificationToken', token);
            }

            fetch('/StaffPages/RequestDetail?handler=AddComment', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }
                    return response.text();
                })
                .then(html => {
                    console.log('✅ Comment added successfully');
                    // Update modal content with refreshed data
                    modalContent.innerHTML = html;
                    // Re-initialize handlers for the new content
                    initializeModalFormHandlers(requestId);
                    showNotification('Comment added successfully!', 'success');
                })
                .catch(error => {
                    console.error('❌ Error adding comment:', error);
                    alert('Error adding comment. Please try again.');
                });

            return false;
        };
    }
}

function showNotification(message, type = 'info') {
    // Create a simple toast notification
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} position-fixed top-0 start-50 translate-middle-x mt-3`;
    toast.style.zIndex = '9999';
    toast.textContent = message;
    document.body.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 3000);
}
