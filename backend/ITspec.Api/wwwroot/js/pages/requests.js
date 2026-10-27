import { api } from '../api.js';
import { h, $, renderNav, requireRole, badge, deviceIcon, timeAgo, showMessage } from '../ui.js';

renderNav();
const user = requireRole('Client', 'Technician', 'Admin');

const titles = {
  Client: 'Моите заявки',
  Technician: 'Отворени заявки за моите устройства',
  Admin: 'Всички заявки',
};
$('#title').textContent = titles[user.role];

if (user.role === 'Client') {
  $('#toolbar').append(h('a', { class: 'button accent', href: '/new-request.html' }, '+ Публикувай заявка'));
}

const list = $('#list');
list.replaceChildren(h('p', { class: 'muted' }, 'Зареждане…'));

try {
  const requests = await api('/requests');

  if (requests.length === 0) {
    const emptyText = {
      Client: 'Още нямате заявки.',
      Technician: 'Няма отворени заявки за вашите видове устройства. Проверете „Моите устройства“.',
      Admin: 'Няма заявки.',
    };
    list.replaceChildren(h('p', { class: 'empty' }, emptyText[user.role]));
  } else {
    list.replaceChildren(
      ...requests.map((r) =>
        h('article', { class: 'listing' },
          deviceIcon(r.deviceTypeName),
          h('div', {},
            h('h3', {}, h('a', { href: `/request.html?id=${r.supportRequestId}` }, r.title)),
            h('div', { class: 'meta' },
              h('span', {}, r.deviceTypeName + (r.deviceModel ? ` · ${r.deviceModel}` : '')),
              h('span', {}, r.city),
              h('span', {}, timeAgo(r.createdAt)),
              user.role !== 'Client' ? h('span', {}, `клиент: ${r.clientName}`) : null,
            ),
          ),
          h('div', { class: 'side' },
            badge(r.status),
            h('span', { class: 'offers-chip' }, r.offersCount === 1 ? '1 оферта' : `${r.offersCount} оферти`),
          ),
        )),
    );
  }
} catch (err) {
  list.replaceChildren();
  showMessage($('#message'), err.message);
}
