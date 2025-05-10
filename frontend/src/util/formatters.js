/**
 * Utility functions for formatting various data types
 */

/**
 * Format a currency value with $ symbol and 2 decimal places
 * @param {number|string} value - The value to format
 * @param {string} [currencySymbol='$'] - Currency symbol to use
 * @param {number} [decimals=2] - Number of decimal places
 * @returns {string} Formatted currency string
 */
export const formatCurrency = (value, currencySymbol = '$', decimals = 2) => {
  if (value === undefined || value === null) return 'N/A';
  return `${currencySymbol}${parseFloat(value).toFixed(decimals)}`;
};

/**
 * Format a date string to a localized date and time
 * @param {string|Date} dateString - The date to format
 * @param {object} [options] - Intl.DateTimeFormat options
 * @returns {string} Formatted date string
 */
export const formatDate = (dateString, options = {}) => {
  if (!dateString) return 'N/A';
  const date = dateString instanceof Date ? dateString : new Date(dateString);
  return date.toLocaleString(undefined, options);
};

/**
 * Format a number with specific decimal places
 * @param {number|string} value - The value to format
 * @param {number} [decimals=4] - Number of decimal places
 * @returns {string} Formatted number string
 */
export const formatNumber = (value, decimals = 4) => {
  if (value === undefined || value === null) return 'N/A';
  return parseFloat(value).toFixed(decimals);
};

/**
 * Get CSS class based on profit value for consistent styling
 * @param {number} profit - The profit value
 * @param {string} [positiveClass='text-success'] - CSS class for positive values
 * @param {string} [negativeClass='text-danger'] - CSS class for negative values
 * @returns {string} CSS class
 */
export const getProfitClass = (profit, positiveClass = 'text-success', negativeClass = 'text-danger') => {
  if (!profit) return '';
  return profit > 0 ? positiveClass : negativeClass;
}; 