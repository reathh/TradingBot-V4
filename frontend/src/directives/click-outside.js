// click-outside.js
export const clickOutside = {
    beforeMount(el, binding) {
        el._clickOutside = (event) => {
            // Check if the clicked element is not the element or one of its children
            if (!(el === event.target || el.contains(event.target))) {
                // Call the provided method
                binding.value(event);
            }
        };
        document.addEventListener('click', el._clickOutside);
    },
    unmounted(el) {
        document.removeEventListener('click', el._clickOutside);
        delete el._clickOutside;
    }
};

export default {
    install(app) {
        app.directive('click-outside', clickOutside);
    }
};