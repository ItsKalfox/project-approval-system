import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5027/api',
  headers: { 'Content-Type': 'application/json' },
});

// Attach JWT token automatically to every request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('pas_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default api;
