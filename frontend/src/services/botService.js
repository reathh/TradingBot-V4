import apiClient from './api';

/**
 * Service for managing bot-related API calls
 */
export default {
    /**
     * Get detailed trading history with profit calculations
     * @param {Object} options - Options for the request
     * @param {number} options.page - Page number to fetch (starting from 1)
     * @param {number} options.pageSize - Number of items per page
     * @param {string} options.period - Time period to filter results
     * @param {number} options.botId - Optional bot ID to filter results by bot
     * @returns {Promise} Promise with trading history
     */
    getTrades({ page = 1, pageSize = 10, period = 'month', botId = null } = {}) {
        return apiClient.get('/trades', {
            params: { page, pageSize, period, botId }
        });
    },

    /**
     * Fetch aggregated statistics and summary metrics for dashboard visualisation.
     */
    getStats(interval = 'Day', botId = null, startDate = null, endDate = null) {
        return apiClient.get('/trades/stats', {
            params: {
                interval,
                botId: botId || undefined,
                startDate: startDate || undefined,
                endDate: endDate || undefined
            }
        });
    }
};