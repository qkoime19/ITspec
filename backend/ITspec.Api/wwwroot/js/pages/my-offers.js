import { api } from '../api.js';
import { h, $, renderNav, requireRole, badge, formatDate, formatPrice, showMessage, OFFER_STATUS } from '../ui.js';

renderNav();
requireRole('Technician');

const list = $('#list');

async function load() {
  const offers = await api('/offers/mine');
  if (offers.length === 0) {
    list.replaceChildren(h('p', { class: 'empty' }, 'Още не сте давали оферти.'));
    return;
  }

  list.replaceChildren(
    ...offers.map((o) => {
      const card = h('div', { class: 'card' },
        h('h3', {}, h('a', { href: `/request.html?id=${o.supportRequestId}` }, o.requestTitle)),
        h('div', { class: 'card-row' },
          h('strong', {}, formatPrice(o.price)),
          'оферта:', badge(o.status, OFFER_STATUS),
          'заявка:', badge(o.requestStatus),
          `изпратена ${formatDate(o.createdAt)}`,
        ),
      );

      const actions = [];
      if (o.status === 'Pending') {
        const withdraw = h('button', { class: 'secondary' }, 'Оттегли');
        withdraw.addEventListener('click', async () => {
          withdraw.disabled = true;
          try {
            await api(`/offers/${o.offerId}/withdraw`, { method: 'POST' });
            await load();
          } catch (err) {
            showMessage($('#message'), err.message);
            withdraw.disabled = false;
          }
        });
        actions.push(withdraw);
      }
      if (o.status === 'Accepted' && o.requestStatus === 'Assigned') {
        actions.push(h('a', { class: 'button', href: `/request.html?id=${o.supportRequestId}` }, 'Към работата'));
      }
      if (actions.length) card.append(h('div', { class: 'actions' }, actions));
      return card;
    }),
  );
}

try {
  await load();
} catch (err) {
  showMessage($('#message'), err.message);
}
