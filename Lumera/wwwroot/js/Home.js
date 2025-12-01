// Home.js - Enhanced with database interactions
document.addEventListener('DOMContentLoaded', function () {
    // Initialize smooth scrolling for navigation links
    initSmoothScrolling();

    // Initialize button interactions
    initButtonInteractions();

    // Initialize scroll animations
    initScrollAnimations();

    // Initialize supplier item clicks
    initSupplierInteractions();

    // Initialize service category clicks
    initServiceCategoryInteractions();
});

// Smooth scrolling for navigation links
function initSmoothScrolling() {
    const navLinks = document.querySelectorAll('a[href^="#"]');

    navLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();

            const targetId = this.getAttribute('href');
            if (targetId === '#') return;

            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                const offsetTop = targetElement.offsetTop - 100;

                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });
}

// Button interactions and event handlers
function initButtonInteractions() {
    // AI Planner button
    const aiPlannerBtn = document.querySelector('.btn-primary');
    if (aiPlannerBtn) {
        aiPlannerBtn.addEventListener('click', function () {
            // Redirect to AI planner page
            window.location.href = '/AiPlanner';
        });
    }

    // Find Organizer button
    const findOrganizerBtn = document.querySelector('.btn-secondary');
    if (findOrganizerBtn) {
        findOrganizerBtn.addEventListener('click', function () {
            window.location.href = '/Organizers';
        });
    }

    // Try AI Planner button
    const tryAiPlannerBtn = document.querySelector('.btn-white');
    if (tryAiPlannerBtn) {
        tryAiPlannerBtn.addEventListener('click', function () {
            window.location.href = '/AiPlanner';
        });
    }

    // Customize Event button
    const customizeBtn = document.querySelector('.btn-outline-white');
    if (customizeBtn) {
        customizeBtn.addEventListener('click', function () {
            window.location.href = '/Events/Create';
        });
    }

    // Navigation CTA button
    const navCtaBtn = document.querySelector('.nav-cta-btn');
    if (navCtaBtn) {
        navCtaBtn.addEventListener('click', function () {
            window.location.href = '/Events/Create';
        });
    }
}

// Supplier item interactions
function initSupplierInteractions() {
    const supplierItems = document.querySelectorAll('.supplier-item');

    supplierItems.forEach(item => {
        item.addEventListener('click', function () {
            const supplierId = this.getAttribute('data-supplier-id');
            if (supplierId) {
                window.location.href = `/Suppliers/Details/${supplierId}`;
            }
        });
    });
}

// Service category interactions
function initServiceCategoryInteractions() {
    const serviceCards = document.querySelectorAll('.service-card');

    serviceCards.forEach(card => {
        card.addEventListener('click', function () {
            const category = this.querySelector('h3').textContent.replace(' Services', '');
            window.location.href = `/Services?category=${encodeURIComponent(category)}`;
        });
    });
}

// Scroll animations for elements
function initScrollAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in');
            }
        });
    }, observerOptions);

    // Observe elements for animation
    const elementsToAnimate = document.querySelectorAll('.feature-card, .service-card, .ai-chat-demo, .supplier-item');
    elementsToAnimate.forEach(el => {
        observer.observe(el);
    });
}

// Load more suppliers (for future pagination)
async function loadMoreSuppliers(category = null) {
    try {
        const response = await fetch(`/api/suppliers?category=${category}`);
        const suppliers = await response.json();

        // Update UI with new suppliers
        updateSuppliersGrid(suppliers);
    } catch (error) {
        console.error('Error loading suppliers:', error);
    }
}

// Utility functions
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Export functions for use in other modules
window.LumeraHome = {
    initSmoothScrolling,
    initButtonInteractions,
    initScrollAnimations,
    initSupplierInteractions,
    initServiceCategoryInteractions,
    loadMoreSuppliers
};