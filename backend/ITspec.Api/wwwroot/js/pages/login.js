import { api, saveLogin } from '../api.js';
import { $, renderNav, showMessage } from '../ui.js';

renderNav();

const form = $('#login-form');
form.addEventListener('submit', async (event) => {
  event.preventDefault();
  const button = form.querySelector('button');
  button.disabled = true;

  try {
    const data = Object.fromEntries(new FormData(form));
    const result = await api('/auth/login', { method: 'POST', body: data });
    saveLogin(result);

    const next = new URLSearchParams(location.search).get('next');
    location.href = next && next.startsWith('/') ? next : '/requests.html';
  } catch (err) {
    showMessage($('#message'), err.message);
  } finally {
    button.disabled = false;
  }
});
