import { api } from '../api.js';
import { h, $, renderNav, requireRole, showMessage } from '../ui.js';

renderNav();
requireRole('Client');

const form = $('#request-form');
const select = form.elements.deviceTypeId;

try {
  const types = await api('/device-types');
  select.replaceChildren(
    h('option', { value: '' }, '— изберете —'),
    ...types.map((t) => h('option', { value: t.deviceTypeId }, t.name)),
  );
} catch (err) {
  showMessage($('#message'), err.message);
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  const button = form.querySelector('button');
  button.disabled = true;

  const data = Object.fromEntries(new FormData(form));
  const body = {
    deviceTypeId: Number(data.deviceTypeId) || 0,
    title: data.title,
    description: data.description,
    deviceModel: data.deviceModel || null,
    city: data.city || null,
    preferredDate: data.preferredDate || null,
  };

  try {
    const created = await api('/requests', { method: 'POST', body });
    location.href = `/request.html?id=${created.supportRequestId}`;
  } catch (err) {
    showMessage($('#message'), err.message);
    button.disabled = false;
  }
});
