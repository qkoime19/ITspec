import { api } from '../api.js';
import { h, $, renderNav, requireRole, showMessage } from '../ui.js';

renderNav();
requireRole('Technician');

const form = $('#types-form');
const checks = $('#checks');

try {
  const [types, mine] = await Promise.all([api('/device-types'), api('/technicians/me/device-types')]);
  checks.replaceChildren(
    ...types.map((t) =>
      h('label', {},
        h('input', { type: 'checkbox', name: 'deviceTypeId', value: t.deviceTypeId, checked: mine.includes(t.deviceTypeId) }),
        t.name)),
  );
} catch (err) {
  showMessage($('#message'), err.message);
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  const button = form.querySelector('button[type=submit]');
  button.disabled = true;

  // Целият списък отива наведнъж -> процедурата го приема като табличен тип.
  const deviceTypeIds = [...form.querySelectorAll('input[name=deviceTypeId]:checked')].map((c) => Number(c.value));

  try {
    await api('/technicians/me/device-types', { method: 'PUT', body: { deviceTypeIds } });
    showMessage($('#message'), 'Запазено.', 'success');
  } catch (err) {
    showMessage($('#message'), err.message);
  } finally {
    button.disabled = false;
  }
});
