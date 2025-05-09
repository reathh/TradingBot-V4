import apiClient from './api';

/**
 * Service for managing bot-related API calls
 */
export default {
    /**
     * Get bot performance chart data for the specified time period
     * @param {string} period - The time period (day, week, month, year)
     * @returns {Promise} Promise with bot performance chart data
     */
    getBotPerformance(period = 'month') {
        return apiClient.get('/dashboard/bot-profits-chart', {
            params: { period }
        });
    },

    /**
     * Get detailed bot trading history with profit calculations
     * @param {Object} options - Options for the request
     * @param {number} options.page - Page number to fetch (starting from 1)
     * @param {number} options.pageSize - Number of items per page
     * @param {string} options.period - Time period to filter results
     * @param {number} options.botId - Optional bot ID to filter results by bot
     * @returns {Promise} Promise with bot trading history
     */
    getBotProfits({ page = 1, pageSize = 10, period = 'month', botId = null } = {}) {
        return apiClient.get('/dashboard/bot-profits', {
            params: { page, pageSize, period, botId }
        });
    },

    /**
     * Calculate bot profit from order data
     * @param {Object} order - Order data with entry and exit prices
     * @returns {Number} - Calculated profit
     */
    calculateProfit(order) {
        if (!order || !order.entryAvgPrice || !order.exitAvgPrice) {
            return 0;
        }

        // Calculate profit: (exit price - entry price) * quantity - fees
        const profit =
            (order.exitAvgPrice - order.entryAvgPrice) * order.quantity -
            (order.entryFee + order.exitFee);

        return parseFloat(profit.toFixed(2));
    }
};