
console.log('staff-queue.js loaded successfully');

let requestDetailModalInstance = null;

document.addEventListener('DOMContentLoaded', () => {
    const modalElement = document.getElementById('requestDetailModal');
    if (!modalElement) {
        return;
    }

    requestDetailModalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);
    modalElement.addEventListener('hidden.bs.modal', cleanupModalBackdrop);
});

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
    
    requestDetailModalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);
    requestDetailModalInstance.show();
    
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
            initializeModalFormHandlers(requestId);
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

function initializeModalFormHandlers(requestId) {
    const modalContent = document.getElementById('modalContent');
    if (!modalContent) {
        return;
    }

    const token = document.querySelector('#antiforgeryForm input[name="__RequestVerificationToken"]')?.value;

    const attachmentForm = modalContent.querySelector('.staff-attachment-form');
    if (attachmentForm) {
        attachmentForm.onsubmit = function (e) {
            e.preventDefault();

            const formData = new FormData(this);
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
                    modalContent.innerHTML = html;
                    initializeModalFormHandlers(requestId);
                    showNotification('Attachment uploaded successfully.', 'success');
                })
                .catch(error => {
                    console.error('Error uploading attachment:', error);
                    showNotification('Unable to upload attachment. Please try again.', 'danger');
                });

            return false;
        };
    }

    const commentForm = modalContent.querySelector('.staff-comment-form');
    if (commentForm) {
        commentForm.onsubmit = function (e) {
            e.preventDefault();

            const formData = new FormData(this);
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
                    modalContent.innerHTML = html;
                    initializeModalFormHandlers(requestId);
                    showNotification('Message sent successfully.', 'success');
                })
                .catch(error => {
                    console.error('Error adding comment:', error);
                    showNotification('Unable to send message. Please try again.', 'danger');
                });

            return false;
        };
    }
}

function showNotification(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} position-fixed top-0 start-50 translate-middle-x mt-3`;
    toast.style.zIndex = '9999';
    toast.textContent = message;
    document.body.appendChild(toast);

    setTimeout(() => {
        toast.remove();
    }, 3000);
}

function cleanupModalBackdrop() {
    document.querySelectorAll('.modal-backdrop').forEach(backdrop => backdrop.remove());
    document.body.classList.remove('modal-open');
    document.body.style.removeProperty('padding-right');
    document.body.style.removeProperty('overflow');
}
