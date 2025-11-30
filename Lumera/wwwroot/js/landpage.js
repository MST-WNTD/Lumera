// wwwroot/js/featureAnimation.js
document.addEventListener("DOMContentLoaded", () => {
    const boxes = document.querySelectorAll(".feature-box");

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add("show");
                observer.unobserve(entry.target); // animate once
            }
        });
    }, { threshold: 0.3 });

    boxes.forEach(box => observer.observe(box));
});
