import { api } from '../api.js';
import {
  h, $, renderNav, requireRole, badge, formatDate, formatPrice, stars,
  showMessage, clearMessage, OFFER_STATUS,
} from '../ui.js';

renderNav();
const user = requireRole('Client', 'Technician', 'Admin');

const id = Number(new URLSearchParams(location.search).get('id'));
const message = $('#message');
const content = $('#content');

// Изпълнява действие, после презарежда детайлите.
async function run(button, action, successText) {
  button.disabled = true;
  clearMessage(message);
  try {
    await action();
    await load();
    if (successText) showMessage(message, successText, 'success');
  } catch (err) {
    showMessage(message, err.message);
    button.disabled = false;
  }
}

async function load() {
  const request = await api(`/requests/${id}`);
  // Офертите вижда само собственикът и Admin (сървърът връща 403 на другите).
  const offers = user.role === 'Client' || user.role === 'Admin'
    ? await api(`/requests/${id}/offers`)
    : [];

  content.replaceChildren(
    h('p', { class: 'muted', style: 'margin:0 0 6px' },
      h('a', { href: '/index.html' }, 'Обяви'), ' › ',
      h('a', { href: `/index.html?type=${request.deviceTypeId}` }, request.deviceTypeName)),
    h('h1', {}, request.title, ' ', badge(request.status)),
    h('div', { class: 'two-col' },
      h('div', {},
        detailsCard(request),
        contactCard(request),
        reviewSection(request),
        offersSection(request, offers),
      ),
      h('aside', {},
        acceptedCard(request),
        clientActions(request),
        technicianSection(request),
      ),
    ),
  );
}

function detailsCard(r) {
  return h('div', { class: 'card' },
    h('dl', { class: 'details' },
      h('dt', {}, 'Устройство'), h('dd', {}, r.deviceTypeName + (r.deviceModel ? ` · ${r.deviceModel}` : '')),
      h('dt', {}, 'Проблем'), h('dd', {}, r.description),
      h('dt', {}, 'Град'), h('dd', {}, r.city),
      h('dt', {}, 'Удобна дата'), h('dd', {}, formatDate(r.preferredDate)),
      h('dt', {}, 'Клиент'), h('dd', {}, r.clientName),
      h('dt', {}, 'Публикувана'), h('dd', {}, formatDate(r.createdAt)),
    ));
}

// Скритите данни: сървърът ги изпраща само на техника с приетата оферта.
function contactCard(r) {
  if (!r.clientPhone && !r.clientAddress) return null;
  return h('div', { class: 'card contact' },
    h('h3', {}, 'Контакт с клиента'),
    h('dl', { class: 'details' },
      h('dt', {}, 'Телефон'), h('dd', {}, r.clientPhone ?? '—'),
      h('dt', {}, 'Адрес'), h('dd', {}, r.clientAddress ?? '—'),
    ));
}

function acceptedCard(r) {
  if (!r.acceptedOffer) return null;
  const a = r.acceptedOffer;
  return h('div', { class: 'card' },
    h('h3', {}, 'Приета оферта'),
    h('div', { class: 'card-row' },
      h('a', { href: `/technician.html?id=${a.technicianId}` }, a.technicianName),
      formatPrice(a.price),
      a.proposedDate ? `дата: ${formatDate(a.proposedDate)}` : null,
    ));
}

function clientActions(r) {
  if (user.role !== 'Client' || !['Open', 'Assigned'].includes(r.status)) return null;
  const cancel = h('button', { class: 'danger' }, 'Прекрати заявката');
  cancel.addEventListener('click', () => {
    if (!window.confirm('Сигурни ли сте, че искате да прекратите заявката?')) return;
    run(cancel, () => api(`/requests/${id}/cancel`, { method: 'POST' }), 'Заявката е прекратена.');
  });
  return h('div', { class: 'actions' }, cancel);
}

function offersSection(r, offers) {
  if (user.role === 'Technician') return null;
  const items = offers.map((o) => {
    const row = h('div', { class: 'card' },
      h('div', { class: 'card-row' },
        h('strong', {}, formatPrice(o.price)),
        h('a', { href: `/technician.html?id=${o.technicianId}` }, o.technicianName),
        stars(o.technicianRating),
        badge(o.status, OFFER_STATUS),
        o.proposedDate ? `дата: ${formatDate(o.proposedDate)}` : null,
      ),
      o.message ? h('p', {}, o.message) : null,
    );
    if (user.role === 'Client' && r.status === 'Open' && o.status === 'Pending') {
      const accept = h('button', {}, 'Приеми');
      accept.addEventListener('click', () =>
        run(accept, () => api(`/offers/${o.offerId}/accept`, { method: 'POST' }), 'Офертата е приета.'));
      row.append(h('div', { class: 'actions' }, accept));
    }
    return row;
  });

  return h('section', {},
    h('h2', {}, `Оферти (${offers.length})`),
    ...(items.length ? items : [h('p', { class: 'empty' }, 'Още няма оферти.')]),
  );
}

function technicianSection(r) {
  if (user.role !== 'Technician') return null;
  const my = r.myOffer;

  // Още няма оферта и заявката е отворена -> форма за оферта.
  if (!my && r.status === 'Open') {
    const form = h('form', { novalidate: true },
      h('label', {}, 'Цена (€)', h('input', { name: 'price', type: 'number', min: '0.01', step: '0.01', required: true })),
      h('label', {}, 'Дата, на която мога', h('input', { name: 'proposedDate', type: 'date' })),
      h('label', {}, 'Съобщение', h('textarea', { name: 'message', maxlength: '1000' })),
      h('button', { type: 'submit' }, 'Изпрати оферта'),
    );
    form.addEventListener('submit', (event) => {
      event.preventDefault();
      const data = Object.fromEntries(new FormData(form));
      const body = {
        price: Number(data.price),
        proposedDate: data.proposedDate || null,
        message: data.message || null,
      };
      run(form.querySelector('button'),
        () => api(`/requests/${id}/offers`, { method: 'POST', body }),
        'Офертата е изпратена.');
    });
    return h('section', {}, h('h2', {}, 'Моята оферта'), form);
  }

  if (!my) return null;

  const actions = [];
  if (my.status === 'Pending') {
    const withdraw = h('button', { class: 'secondary' }, 'Оттегли офертата');
    withdraw.addEventListener('click', () =>
      run(withdraw, () => api(`/offers/${my.offerId}/withdraw`, { method: 'POST' }), 'Офертата е оттеглена.'));
    actions.push(withdraw);
  }
  if (my.status === 'Accepted' && r.status === 'Assigned') {
    const complete = h('button', {}, 'Работата е свършена');
    complete.addEventListener('click', () =>
      run(complete, () => api(`/requests/${id}/complete`, { method: 'POST' }), 'Заявката е завършена.'));
    actions.push(complete);
  }

  return h('section', {},
    h('h2', {}, 'Моята оферта'),
    h('div', { class: 'card' },
      h('div', { class: 'card-row' },
        h('strong', {}, formatPrice(my.price)),
        badge(my.status, OFFER_STATUS),
        my.proposedDate ? `дата: ${formatDate(my.proposedDate)}` : null,
      ),
      my.message ? h('p', {}, my.message) : null,
      actions.length ? h('div', { class: 'actions' }, actions) : null,
    ));
}

function reviewSection(r) {
  if (r.review) {
    return h('section', {},
      h('h2', {}, 'Отзив'),
      h('div', { class: 'card' },
        h('div', {}, stars(r.review.rating)),
        r.review.comment ? h('p', {}, r.review.comment) : null,
      ));
  }

  if (user.role !== 'Client' || r.status !== 'Completed') return null;

  const form = h('form', { novalidate: true },
    h('label', {}, 'Оценка',
      h('select', { name: 'rating', required: true },
        ...[5, 4, 3, 2, 1].map((n) => h('option', { value: n }, `${n} ${'★'.repeat(n)}`)))),
    h('label', {}, 'Коментар', h('textarea', { name: 'comment', maxlength: '1000' })),
    h('button', { type: 'submit' }, 'Изпрати отзив'),
  );
  form.addEventListener('submit', (event) => {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(form));
    run(form.querySelector('button'),
      () => api(`/requests/${id}/review`, {
        method: 'POST',
        body: { rating: Number(data.rating), comment: data.comment || null },
      }),
      'Благодарим за отзива!');
  });
  return h('section', {}, h('h2', {}, 'Оставете отзив'), form);
}

try {
  await load();
} catch (err) {
  showMessage(message, err.message);
}
