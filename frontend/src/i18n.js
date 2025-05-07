import { createI18n } from "vue-i18n";

function loadLocaleMessages() {
  const locales = import.meta.globEager("./locales/*.json"); // Vite's way of dynamic import
  const messages = {};
  for (const path in locales) {
    const matched = path.match(/([a-z0-9-_]+)\./i);
    if (matched && matched.length > 1) {
      const locale = matched[1];
      messages[locale] = locales[path].default; // Access the default export in Vite
    }
  }
  return messages;
}

const i18n = createI18n({
  locale: import.meta.env.VUE_APP_I18N_LOCALE || "en", // Access environment variables in Vite
  fallbackLocale: import.meta.env.VUE_APP_I18N_FALLBACK_LOCALE || "en",
  messages: loadLocaleMessages(),
});

export default i18n;
