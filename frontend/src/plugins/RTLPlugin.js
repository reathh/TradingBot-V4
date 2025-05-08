import { ref, provide, inject } from 'vue';

// Create a reactive RTL state
const isRTL = ref(false);

// Create the RTL store
const rtlStore = {
    isRTL,
    toggleRTL() {
        isRTL.value = !isRTL.value;
        document.documentElement.dir = isRTL.value ? 'rtl' : 'ltr';

        // Dispatch an event to notify other components
        window.dispatchEvent(new CustomEvent('rtl-change', {
            detail: { rtl: isRTL.value }
        }));
    },
    setRTL(value) {
        isRTL.value = value;
        document.documentElement.dir = isRTL.value ? 'rtl' : 'ltr';
    }
};

// Create a composable to access the RTL store
export function useRTL() {
    return inject('$rtl', rtlStore);
}

// Export the plugin
export default {
    install(app) {
        // Provide the RTL store
        app.provide('$rtl', rtlStore);

        // Add the RTL store to the global properties
        app.config.globalProperties.$rtl = rtlStore;
    }
};