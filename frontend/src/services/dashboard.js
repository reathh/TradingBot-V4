import apiClient from './api';

export default {
    /**
     * Get dashboard summary data
     * @returns {Promise} Promise object that resolves to dashboard summary
     */
    getSummary() {
        return apiClient.get('/dashboard/summary');
    },

    /**
     * Get performance data for charts
     * @returns {Promise} Promise object that resolves to performance data
     */
    getPerformance() {
        return apiClient.get('/dashboard/performance');
    },

    /**
     * Get recent trades
     * @returns {Promise} Promise object that resolves to recent trades
     */
    getRecentTrades() {
        return apiClient.get('/dashboard/recent-trades');
    },

    /**
     * Get tasks
     * @returns {Promise} Promise object that resolves to task list
     */
    getTasks() {
        return apiClient.get('/dashboard/tasks');
    }
};