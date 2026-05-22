document.addEventListener('DOMContentLoaded', function() {
    const targets = document.querySelectorAll('.fade-in-up, .fade-in, .fade-in-delay, .stagger-children > *');
    if (!targets.length) return;

    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });

    document.querySelectorAll('.fade-in-up, .fade-in, .fade-in-delay').forEach(function(el) {
        observer.observe(el);
    });

    document.querySelectorAll('.stagger-children').forEach(function(parent) {
        Array.from(parent.children).forEach(function(child, i) {
            child.style.transitionDelay = (i * 0.1) + 's';
            child.classList.add('fade-in-up');
            observer.observe(child);
        });
    });
});
