import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useThemeStore = defineStore('theme', () => {
  // Get the initial mode from localStorage or default to dark mode (true)
  const isDarkMode = ref(localStorage.getItem('darkMode') !== 'false');
  
  function toggleDarkMode() {
    isDarkMode.value = !isDarkMode.value;
    updateTheme();
    // Save to localStorage
    localStorage.setItem('darkMode', isDarkMode.value.toString());
  }
  
  function setDarkMode(value) {
    isDarkMode.value = value;
    updateTheme();
    // Save to localStorage
    localStorage.setItem('darkMode', isDarkMode.value.toString());
  }
  
  function updateTheme() {
    const docClasses = document.body.classList;
    if (isDarkMode.value) {
      docClasses.remove('white-content');
    } else {
      docClasses.add('white-content');
    }
  }
  
  // Initialize theme on store creation
  updateTheme();
  
  return {
    isDarkMode,
    toggleDarkMode,
    setDarkMode
  };
}); 