import {defineStore} from 'pinia';
import {computed, ref} from 'vue';
import {authService} from '@/services/auth';
import router from '@/router';

export const useAuthStore = defineStore( 'auth', () =>
{
  const user = ref( null );
  const token = ref( localStorage.getItem( 'token' ) || null );
  const loading = ref( false );
  const error = ref( null );

  const isAuthenticated = computed( () => !!token.value );

  async function login ( credentials )
  {
    try
    {
      loading.value = true;
      error.value = null;
      const response = await authService.login( credentials );
      token.value = response.token;
      user.value = response.user;
      localStorage.setItem( 'token', response.token );
      await router.push('/dashboard');
    } catch ( err )
    {
      error.value = err.response?.data?.message || 'Login failed';
      throw error.value;
    } finally
    {
      loading.value = false;
    }
  }

  async function register ( userData )
  {
    try
    {
      loading.value = true;
      error.value = null;
      const response = await authService.register( userData );
      token.value = response.token;
      user.value = response.user;
      localStorage.setItem( 'token', response.token );
      router.push( '/dashboard' );
    } catch ( err )
    {
      error.value = err.response?.data?.message || 'Registration failed';
      throw error.value;
    } finally
    {
      loading.value = false;
    }
  }

  async function logout ()
  {
    token.value = null;
    user.value = null;
    localStorage.removeItem( 'token' );
    await router.push( '/login' );
  }

  async function checkAuth ()
  {
    if ( token.value )
    {
      try
      {
        user.value = await authService.getCurrentUser();
      } catch ( err )
      {
        token.value = null;
        user.value = null;
        localStorage.removeItem( 'token' );
      }
    }
  }

  return {
    user,
    token,
    loading,
    error,
    isAuthenticated,
    login,
    register,
    logout,
    checkAuth
  };
} );
