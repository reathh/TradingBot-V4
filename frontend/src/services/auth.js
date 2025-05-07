import apiClient from './api';

export const authService = {
  async login(credentials) {
    try {
      const response = await apiClient.post('/auth/login', credentials);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  async register(userData) {
    try {
      const response = await apiClient.post('/auth/register', userData);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  async getCurrentUser() {
    try {
      const response = await apiClient.get('/auth/me');
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  async refreshToken() {
    try {
      const response = await apiClient.post('/auth/refresh-token');
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  async logout() {
    try {
      await apiClient.post('/auth/logout');
      return true;
    } catch (error) {
      throw error;
    }
  }
};
