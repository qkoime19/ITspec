import { api } from '../api.js';
import { h, $, renderNav, requireRole, formatDate, showMessage, clearMessage, ROLE_NAME } from '../ui.js';

renderNav();
requireRole('Admin');

const content = $('#tab-content');
const message = $('#message');

const tabs = { types: renderTypes, users: renderUsers, stats: renderStats };

document.querySelectorAll('[data-tab]').forEach((button) => {
  button.addEventListener('click', () => {
    document.querySelectorAll('[data-tab]').forEach((b) => b.classList.toggle('active', b === button));
    show(button.dataset.tab);
  });
});

async function show(tab) {
  clearMessage(message);
  try {
    await tabs[tab]();
  } catch (err) {
    showMessage(message, err.message);
  }
}

// ---------- Видове устройства ----------
async function renderTypes() {
  const types = await api('/device-types?includeInactive=true');

  const form = h('form', { novalidate: true, style: 'grid-template-columns: 1fr auto; max-width: 520px' },
    h('input', { name: 'name', required: true, placeholder: 'Нов вид устройство', 'aria-label': 'Име' }),
    h('button', { type: 'submit' }, 'Добави'),
  );
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    try {
      await api('/device-types', { method: 'POST', body: { name: form.elements.name.value, isActive: true } });
      await show('types');
    } catch (err) {
      showMessage(message, err.message);
    }
  });

  const rows = types.map((t) => {
    const toggle = h('button', { class: t.isActive ? 'secondary' : '' }, t.isActive ? 'Деактивирай' : 'Активирай');
    toggle.addEventListener('click', async () => {
      try {
        if (t.isActive) {
          await api(`/device-types/${t.deviceTypeId}`, { method: 'DELETE' });
        } else {
          await api(`/device-types/${t.deviceTypeId}`, { method: 'PUT', body: { name: t.name, isActive: true } });
        }
        await show('types');
      } catch (err) {
        showMessage(message, err.message);
      }
    });

    const rename = h('button', { class: 'link' }, 'Преименувай');
    rename.addEventListener('click', async () => {
      const name = window.prompt('Ново име:', t.name);
      if (!name) return;
      try {
        await api(`/device-types/${t.deviceTypeId}`, { method: 'PUT', body: { name, isActive: t.isActive } });
        await show('types');
      } catch (err) {
        showMessage(message, err.message);
      }
    });

    return h('tr', {},
      h('td', {}, t.name),
      h('td', {}, t.isActive ? 'активен' : 'неактивен'),
      h('td', {}, h('div', { class: 'actions', style: 'margin:0' }, rename, toggle)));
  });

  content.replaceChildren(
    form,
    h('div', { class: 'table-wrap', style: 'margin-top:16px' },
      h('table', {},
        h('thead', {}, h('tr', {}, h('th', {}, 'Име'), h('th', {}, 'Състояние'), h('th', {}, ''))),
        h('tbody', {}, rows))),
  );
}

// ---------- Потребители ----------
async function renderUsers() {
  const users = await api('/admin/users');
  const rows = users.map((u) => {
    let action = null;
    if (u.role !== 'Admin') {
      action = h('button', { class: u.isBlocked ? '' : 'danger' }, u.isBlocked ? 'Отблокирай' : 'Блокирай');
      action.addEventListener('click', async () => {
        try {
          await api(`/admin/users/${u.userId}/${u.isBlocked ? 'unblock' : 'block'}`, { method: 'POST' });
          await show('users');
        } catch (err) {
          showMessage(message, err.message);
        }
      });
    }
    return h('tr', {},
      h('td', {}, u.fullName),
      h('td', {}, u.email),
      h('td', {}, ROLE_NAME[u.role] ?? u.role),
      h('td', {}, u.city),
      h('td', {}, formatDate(u.createdAt)),
      h('td', {}, u.isBlocked ? 'блокиран' : 'активен'),
      h('td', {}, action));
  });

  content.replaceChildren(
    h('div', { class: 'table-wrap' },
      h('table', {},
        h('thead', {}, h('tr', {}, ...['Име', 'Имейл', 'Роля', 'Град', 'Регистриран', 'Състояние', ''].map((t) => h('th', {}, t)))),
        h('tbody', {}, rows))),
  );
}

// ---------- Статистика ----------
async function renderStats() {
  const s = await api('/admin/stats');
  const items = [
    ['Клиенти', s.clients],
    ['Техници', s.technicians],
    ['Отворени заявки', s.openRequests],
    ['Възложени заявки', s.assignedRequests],
    ['Завършени заявки', s.completedRequests],
    ['Прекратени заявки', s.cancelledRequests],
    ['Оферти', s.offers],
    ['Отзиви', s.reviews],
  ];
  content.replaceChildren(
    h('div', { class: 'grid' },
      items.map(([label, value]) =>
        h('div', { class: 'card' }, h('div', { class: 'muted' }, label), h('div', { style: 'font-size:1.8rem;font-weight:700' }, String(value))))),
  );
}

show('types');
