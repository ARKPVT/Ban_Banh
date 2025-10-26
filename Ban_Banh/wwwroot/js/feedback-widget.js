// wwwroot/js/feedback-modal.js
document.addEventListener('DOMContentLoaded', () => {
    const dropZone = document.getElementById('fbDrop');    // <label class="drop-zone">
    const fileInput = document.getElementById('fbFiles');   // <input type="file" multiple>
    const preview = document.getElementById('fbPreview'); // vùng hiển thị ảnh
    const alertEl = document.getElementById('fbAlert');   // alert trong modal

    const nameEl = document.getElementById('fbName');
    const emailEl = document.getElementById('fbEmail');
    const ratingEl = document.getElementById('fbRating');
    const msgEl = document.getElementById('fbMessage');

    const sendBtn = document.getElementById('fbSend');
    const sendText = document.getElementById('fbSendText') || sendBtn; // fallback
    const spinnerEl = document.getElementById('fbSpinner');

    if (!dropZone || !fileInput || !preview || !sendBtn) return;

    const MAX_FILES = 5;
    const MAX_MB = 5;

    /** files người dùng chọn (để render & xoá) */
    let selectedFiles = [];
    /** DataTransfer để cập nhật lại fileInput.files khi xoá */
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
            // Giải phóng URL khi img load xong để đỡ leak bộ nhớ
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

    // --- Chọn ảnh bằng hộp thoại
    fileInput.addEventListener('change', (e) => {
        addFiles(e.target.files);
        // reset để chọn lại cùng 1 ảnh vẫn nhận change
        fileInput.value = '';
    });

    // --- Kéo thả ảnh
    ['dragenter', 'dragover'].forEach(ev =>
        dropZone.addEventListener(ev, (e) => {
            e.preventDefault();
            e.stopPropagation();
            dropZone.classList.add('dragover');
        })
    );
    ['dragleave', 'drop'].forEach(ev =>
        dropZone.addEventListener(ev, (e) => {
            e.preventDefault();
            e.stopPropagation();
            dropZone.classList.remove('dragover');
        })
    );
    dropZone.addEventListener('drop', (e) => {
        addFiles(e.dataTransfer.files);
    });

    // --- Xoá 1 ảnh trong preview (event delegation)
    preview.addEventListener('click', (e) => {
        const btn = e.target.closest('button[data-rm]');
        if (!btn) return;
        const idx = Number(btn.dataset.rm);
        selectedFiles.splice(idx, 1);
        syncFileInput();
        renderPreviews();
    });

    // --- Gửi góp ý (multipart/form-data)
    sendBtn.addEventListener('click', async () => {
        hideAlert();

        const name = (nameEl.value || '').trim();
        const email = (emailEl.value || '').trim();
        const rating = Number(ratingEl.value || 5);
        const msg = (msgEl.value || '').trim();

        if (!msg) { showAlert('Vui lòng nhập nội dung góp ý.'); return; }

        try {
            sendBtn.disabled = true;
            if (spinnerEl) spinnerEl.classList.remove('d-none');
            if (sendText && sendText !== sendBtn) sendText.textContent = 'Đang gửi...';

            const fd = new FormData();
            fd.append('name', name);
            fd.append('email', email);
            fd.append('rating', isNaN(rating) ? 5 : rating);
            fd.append('message', msg);
            selectedFiles.forEach((f, i) => fd.append('files', f, f.name));

            const r = await fetch('/api/feedback', { method: 'POST', body: fd });
            const data = await r.json().catch(() => ({}));

            if (!r.ok) throw new Error(data?.error || 'Gửi góp ý thất bại');

            showAlert(`Cảm ơn bạn! Mã góp ý: ${data?.id ?? '(không có mã)'}`, 'success');

            // reset form
            selectedFiles = [];
            syncFileInput();
            renderPreviews();
            nameEl.value = ''; emailEl.value = ''; ratingEl.value = '5'; msgEl.value = '';

            // đóng modal sau 1.2s
            setTimeout(() => {
                const modalEl = document.getElementById('feedbackModal');
                const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
                modal.hide();
                hideAlert();
            }, 1200);

        } catch (err) {
            showAlert(err.message || String(err));
        } finally {
            sendBtn.disabled = false;
            if (spinnerEl) spinnerEl.classList.add('d-none');
            if (sendText && sendText !== sendBtn) sendText.textContent = 'Gửi góp ý';
        }
    });
});
