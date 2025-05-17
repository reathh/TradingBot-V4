import axios from 'axios';
import config from '@/config';
import router from '@/router';

// Use the baseURL from config
const baseURL = `${config.apiBaseUrl}/api`;

const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  },
  timeout: 10000
});

// Add a request interceptor
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add a response interceptor
apiClient.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    // Handle 401 Unauthorized errors in a smarter way
    if (error.response && error.response.status === 401) {
      const { config: requestConfig } = error;
      const isLoginRequest = requestConfig?.url?.includes('/auth/login');
      const storedToken = localStorage.getItem('token');

      // If the request was NOT the login attempt itself and we had a token, force a logout redirect
      if (!isLoginRequest && storedToken) {
        localStorage.removeItem('token');
        // Use vue-router to avoid a full page reload
        router.push('/login');
      }
      // Otherwise (failed login attempt) just propagate the error so the calling code can handle it
    }

    // Log error information to help with debugging
    console.error('API Error:', error.message);
    if (error.response) {
      console.error('Status:', error.response.status);
      console.error('Data:', error.response.data);
    }

    return Promise.reject(error);
  }
);

export default apiClient;
