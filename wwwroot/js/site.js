// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Session timeout handling
let timeout;
const timeoutDuration = 60000; // 1 minute

function resetTimeout() {
    clearTimeout(timeout);
    timeout = setTimeout(() => {
        window.location.href = '/Account/Login';
    }, timeoutDuration);
}

// Reset timeout on user activity
document.addEventListener('mousemove', resetTimeout);
document.addEventListener('keypress', resetTimeout);
document.addEventListener('click', resetTimeout);
document.addEventListener('scroll', resetTimeout);

// Initialize timeout on page load
resetTimeout();

// Photo preview functionality
function previewImage(input) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const preview = document.querySelector('.photo-preview');
            if (preview) {
                preview.src = e.target.result;
                preview.style.display = 'block';
            }
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// Add car animation
document.addEventListener('DOMContentLoaded', function() {
    const carAnimation = document.createElement('div');
    carAnimation.innerHTML = `
        <svg class="car-animation" viewBox="0 0 60 30">
            <path d="M10,20 L50,20 C55,20 58,17 58,15 L58,12 C58,10 55,8 50,8 L10,8 C5,8 2,10 2,12 L2,15 C2,17 5,20 10,20 Z" fill="#333"/>
            <circle cx="15" cy="20" r="5" fill="#666"/>
            <circle cx="45" cy="20" r="5" fill="#666"/>
        </svg>
    `;
    document.body.appendChild(carAnimation);
});

// Toast notifications
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type} show`;
    toast.innerHTML = `
        <div class="toast-header">
            <strong class="me-auto">${type === 'success' ? 'Success' : 'Error'}</strong>
            <button type="button" class="btn-close" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
        <div class="toast-body">${message}</div>
    `;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

// Loading spinner
function showLoading() {
    const spinner = document.createElement('div');
    spinner.className = 'loading-spinner';
    document.body.appendChild(spinner);
}

function hideLoading() {
    const spinner = document.querySelector('.loading-spinner');
    if (spinner) spinner.remove();
}

// Handle form submissions
document.addEventListener('submit', function(e) {
    if (e.target.tagName === 'FORM') {
        showLoading();
    }
});

// Enhanced dropdown styling
document.addEventListener('DOMContentLoaded', function() {
    const selects = document.querySelectorAll('select');
    selects.forEach(select => {
        select.addEventListener('change', function() {
            this.style.borderColor = '#258cfb';
        });
    });
});
