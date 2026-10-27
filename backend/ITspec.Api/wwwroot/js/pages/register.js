import { api } from '../api.js';
import { $, renderNav, showMessage } from '../ui.js';

renderNav();

const form = $('#register-form');
form.addEventListener('submit', async (event) => {
  event.preventDefault();
  const button = form.querySelector('button');
  button.disabled = true;

  const { kind, ...data } = Object.fromEntries(new FormData(form));
  // Ролята НЕ се праща в тялото: тя се определя от адреса.
  const path = kind === 'technician' ? '/auth/register/technician' : '/auth/register/client';

  try {
    await api(path, { method: 'POST', body: data });
    showMessage($('#message'), 'Регистрацията е успешна. Пренасочване към входа…', 'success');
    setTimeout(() => (location.href = '/login.html'), 1200);
  } catch (err) {
    showMessage($('#message'), err.message);
  } finally {
    button.disabled = false;
  }
});
