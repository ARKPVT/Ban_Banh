// wwwroot/js/feedback-modal.js
document.addEventListener('DOMContentLoaded', () => {
    const dropZone = document.getElementById('fbDrop');
    const fileInput = document.getElementById('fbFiles');
    const preview = document.getElementById('fbPreview');
    const alertEl = document.getElementById('fbAlert');

    const nameEl = document.getElementById('fbName');
    const emailEl = document.getElementById('fbEmail');
    const ratingEl = document.getElementById('fbRating');
    const msgEl = document.getElementById('fbMessage');

    const sendBtn = document.getElementById('fbSend');
    const sendText = document.getElementById('fbSendText') || sendBtn;
    const spinnerEl = document.getElementById('fbSpinner');

    if (!dropZone || !fileInput || !preview || !sendBtn) return;

    const MAX_FILES = 5;
    const MAX_MB = 5;

    let selectedFiles = [];
    let dt = new DataTransfer();

    function showAlert(msg, type = 'danger') {
        alertEl.className = `alert alert-${type}`;
        alertEl.textContent = msg;
        alertEl.classList.remove('d-none');
    }
    function hideAlert() {
        alertEl.className = 'alert d-none';
        alertEl.textContent = '';
    }

    function renderPreviews() {
        preview.innerHTML = '';
        selectedFiles.forEach((f, idx) => {
            const url = URL.createObjectURL(f);
            const wrap = document.createElement('span');
            wrap.className = 'thumb';
            wrap.innerHTML = `
        <img src="${url}" alt="preview">
        <button type="button" data-rm="${idx}" aria-label="Xóa">×</button>
      `;
            wrap.querySelector('img').addEventListener('load', () => URL.revokeObjectURL(url));
            preview.appendChild(wrap);
        });
    }

    function syncFileInput() {
        dt = new DataTransfer();
        selectedFiles.forEach(f => dt.items.add(f));
        fileInput.files = dt.files;
    }

    function addFiles(fileList) {
        hideAlert();
        const files = Array.from(fileList);
        for (const f of files) {
            if (!f.type.startsWith('image/')) {
                showAlert('Chỉ nhận tập tin hình ảnh (jpg, png, webp, ...).');
                continue;
            }
            if (f.size > MAX_MB * 1024 * 1024) {
                showAlert(`Ảnh ${f.name} vượt quá ${MAX_MB}MB.`);
                continue;
            }
            if (selectedFiles.length >= MAX_FILES) {
                showAlert(`Chỉ được chọn tối đa ${MAX_FILES} ảnh.`);
                break;
            }
            selectedFiles.push(f);
        }
        syncFileInput();
        renderPreviews();
    }

    // chọn file
    fileInput.addEventListener('change', (e) => {
        addFiles(e.target.files);
        fileInput.value = ''; // cho phép chọn lại cùng file
    });

    // kéo thả
    ['dragenter', 'dragover'].forEach(ev =>
        dropZone.addEventListener(ev, (e) => {
            e.preventDefault(); e.stopPropagation();
            dropZone.classList.add('dragover');
        })
    );
    ['dragleave', 'drop'].forEach(ev =>
        dropZone.addEventListener(ev, (e) => {
            e.preventDefault(); e.stopPropagation();
            dropZone.classList.remove('dragover');
        })
    );
    dropZone.addEventListener('drop', (e) => addFiles(e.dataTransfer.files));

    // xóa thumbnail
    preview.addEventListener('click', (e) => {
        const btn = e.target.closest('button[data-rm]');
        if (!btn) return;
        const idx = Number(btn.dataset.rm);
        selectedFiles.splice(idx, 1);
        syncFileInput();
        renderPreviews();
    });

    // gửi form
    sendBtn.addEventListener('click', async () => {
        hideAlert();

        const name = (nameEl.value || '').trim();
        const email = (emailEl.value || '').trim();
        const rating = Number(ratingEl.value || 5);
        const msg = (msgEl.value || '').trim();  // dùng biến `msg`

        if (!msg) { showAlert('Vui lòng nhập nội dung góp ý.'); return; }

        try {
            sendBtn.disabled = true;
            if (spinnerEl) spinnerEl.classList.remove('d-none');
            if (sendText && sendText !== sendBtn) sendText.textContent = 'Đang gửi...';

            const fd = new FormData();
            fd.append('Name', name);
            fd.append('Email', email);
            fd.append('Rating', isNaN(rating) ? 5 : rating);
            fd.append('Message', msg);                 // key đúng với model
            selectedFiles.forEach(f => fd.append('Files', f, f.name));

            const r = await fetch('/api/feedback', { method: 'POST', body: fd });
            const data = await r.json().catch(() => ({}));
            if (!r.ok) throw new Error(data?.error || 'Gửi góp ý thất bại');

            showAlert(`Cảm ơn bạn! Mã góp ý: ${data?.id ?? ''}`, 'success');

            // reset
            selectedFiles = [];
            syncFileInput();
            renderPreviews();
            nameEl.value = ''; emailEl.value = '';
            ratingEl.value = '5'; msgEl.value = '';

            // đóng modal sau 1.2s (nếu dùng Bootstrap 5)
            setTimeout(() => {
                const modalEl = document.getElementById('feedbackModal');
                if (window.bootstrap?.Modal) {
                    const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
                    modal.hide();
                }
                hideAlert();
            }, 1200);

        } catch (e) {
            showAlert(e.message || String(e));
        } finally {
            sendBtn.disabled = false;
            if (spinnerEl) spinnerEl.classList.add('d-none');
            if (sendText && sendText !== sendBtn) sendText.textContent = 'Gửi góp ý';
        }
    });
});
