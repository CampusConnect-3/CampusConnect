function showRequestDetail(requestId) {
    // Load request detail via AJAX
    fetch(`/StaffPages/RequestDetail?requestId=${requestId}`)
        .then(response => response.text())
        .then(html => {
            document.getElementById('modalContent').innerHTML = html;
            initializeStaffCommentForms();
            const modal = new bootstrap.Modal(document.getElementById('requestDetailModal'));
            modal.show();
        })
        .catch(error => console.error('Error:', error));
}

function initializeStaffCommentForms() {
    const forms = document.querySelectorAll('.staff-comment-form');

    forms.forEach(form => {
        if (form.dataset.initialized === 'true') {
            return;
        }

        form.dataset.initialized = 'true';

        const textarea = form.querySelector('.staff-comment-textarea');
        const submitButton = form.querySelector('.staff-comment-submit');

        let isSubmitting = false;

        function setSubmittingState(submitting) {
            if (!textarea || !submitButton) {
                return;
            }

            isSubmitting = submitting;
            textarea.readOnly = submitting;
            submitButton.disabled = submitting;
            submitButton.textContent = submitting ? 'Sending...' : 'Send';
        }

        async function sendComment() {
            if (!textarea || !submitButton || isSubmitting) {
                return;
            }

            if (!textarea.value.trim()) {
                return;
            }

            setSubmittingState(true);

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form)
                });

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const html = await response.text();
                const modalContent = document.getElementById('modalContent');

                if (modalContent) {
                    modalContent.innerHTML = html;
                    initializeStaffCommentForms();
                }
            } catch (error) {
                console.error('Error sending comment:', error);
                alert('Error sending comment. Please try again.');
                setSubmittingState(false);
            }
        }

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            sendComment();
        });

        if (textarea) {
            textarea.addEventListener('keydown', function (event) {
                if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault();
                    sendComment();
                }
            });
        }
    });
}
