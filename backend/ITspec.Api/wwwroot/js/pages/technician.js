import { api } from '../api.js';
import { h, $, renderNav, stars, formatDate, showMessage } from '../ui.js';

renderNav();

const id = Number(new URLSearchParams(location.search).get('id'));

try {
  const { profile, reviews } = await api(`/technicians/${id}`);
  $('#content').replaceChildren(
    h('h1', {}, profile.fullName),
    h('div', { class: 'card' },
      h('dl', { class: 'details' },
        h('dt', {}, 'Град'), h('dd', {}, profile.city),
        h('dt', {}, 'Устройства'), h('dd', {}, profile.deviceTypes ?? '—'),
        h('dt', {}, 'Рейтинг'), h('dd', {}, stars(profile.avgRating)),
        h('dt', {}, 'Отзиви'), h('dd', {}, String(profile.reviewsCount)),
        h('dt', {}, 'Завършени заявки'), h('dd', {}, String(profile.completedCount)),
      )),
    h('h2', {}, 'Отзиви'),
    ...(reviews.length
      ? reviews.map((r) =>
          h('div', { class: 'card' },
            h('div', { class: 'card-row' }, stars(r.rating), r.requestTitle, formatDate(r.createdAt)),
            r.comment ? h('p', {}, r.comment) : null))
      : [h('p', { class: 'empty' }, 'Още няма отзиви.')]),
  );
} catch (err) {
  showMessage($('#message'), err.message);
}
