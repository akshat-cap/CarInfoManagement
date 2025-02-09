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

// Premium UI Enhancements
document.addEventListener('DOMContentLoaded', function() {
    // Configure Toastr notifications
    toastr.options = {
        "closeButton": true,
        "progressBar": true,
        "positionClass": "toast-bottom-right",
        "preventDuplicates": true,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "5000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Enhanced dropdown menus
    const dropdowns = document.querySelectorAll('.dropdown-toggle');
    dropdowns.forEach(dropdown => {
        dropdown.addEventListener('show.bs.dropdown', function () {
            this.classList.add('active');
        });
        dropdown.addEventListener('hide.bs.dropdown', function () {
            this.classList.remove('active');
        });
    });

    // Form validation styling
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(form => {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
                // Add premium shake animation to invalid fields
                form.querySelectorAll(':invalid').forEach(field => {
                    field.classList.add('shake-animation');
                    setTimeout(() => field.classList.remove('shake-animation'), 500);
                });
            }
            form.classList.add('was-validated');
        });
    });

    // Enhanced image loading
    const images = document.querySelectorAll('img[loading="lazy"]');
    images.forEach(img => {
        img.addEventListener('load', function() {
            this.classList.add('fade-in');
        });
    });

    // Premium hover effects for cards
    const cards = document.querySelectorAll('.card');
    cards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-10px)';
            this.style.boxShadow = '0 10px 20px rgba(0,0,0,0.2)';
        });
        card.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
            this.style.boxShadow = 'none';
        });
    });

    // Animated counters for statistics
    function animateValue(element, start, end, duration) {
        if (start === end) return;
        const range = end - start;
        const increment = end > start ? 1 : -1;
        const stepTime = Math.abs(Math.floor(duration / range));
        let current = start;
        const timer = setInterval(() => {
            current += increment;
            element.textContent = current.toLocaleString();
            if (current === end) {
                clearInterval(timer);
            }
        }, stepTime);
    }

    // Initialize number animations when elements come into view
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting && entry.target.classList.contains('animate-number')) {
                const end = parseInt(entry.target.getAttribute('data-value'));
                animateValue(entry.target, 0, end, 2000);
                observer.unobserve(entry.target);
            }
        });
    });

    document.querySelectorAll('.animate-number').forEach(number => observer.observe(number));

    // Premium button hover effects
    const buttons = document.querySelectorAll('.btn-primary, .btn-outline-primary');
    buttons.forEach(button => {
        button.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px)';
        });
        button.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });

    // Floating labels enhancement
    const floatingInputs = document.querySelectorAll('.form-floating input, .form-floating textarea');
    floatingInputs.forEach(input => {
        if (input.value) {
            input.parentElement.classList.add('filled');
        }
        input.addEventListener('focus', () => {
            input.parentElement.classList.add('focused');
        });
        input.addEventListener('blur', () => {
            input.parentElement.classList.remove('focused');
            if (input.value) {
                input.parentElement.classList.add('filled');
            } else {
                input.parentElement.classList.remove('filled');
            }
        });
    });

    // Initialize tooltips
    const tooltips = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltips.forEach(tooltip => {
        new bootstrap.Tooltip(tooltip);
    });
});
