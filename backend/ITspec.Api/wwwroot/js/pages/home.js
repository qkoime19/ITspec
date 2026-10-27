import { api, getUser } from '../api.js';
import { h, $, renderNav, deviceIcon, timeAgo, showMessage } from '../ui.js';

renderNav();

const user = getUser();
const params = new URLSearchParams(location.search);
const filters = {
  type: params.get('type') ? Number(params.get('type')) : null,
  q: params.get('q') ?? '',
  city: params.get('city') ?? '',
};

function urlWith(changes) {
  const p = new URLSearchParams(location.search);
  for (const [k, v] of Object.entries(changes)) {
    if (v === null || v === '') p.delete(k); else p.set(k, v);
  }
  const qs = p.toString();
  return '/index.html' + (qs ? `?${qs}` : '');
}

function listingRow(r) {
  const href = `/request.html?id=${r.supportRequestId}`;
  // Без вход: обявата се вижда, но за детайлите и офертата трябва вход.
  const target = user ? href : `/login.html?next=${encodeURIComponent(href)}`;

  let action;
  if (!user) action = h('a', { class: 'button accent', href: target }, 'Дай оферта');
  else if (user.role === 'Technician') action = h('a', { class: 'button accent', href }, 'Дай оферта');
  else action = h('a', { class: 'button secondary', href }, 'Виж');

  return h('article', { class: 'listing' },
    deviceIcon(r.deviceTypeName),
    h('div', {},
      h('h3', {}, h('a', { href: target }, r.title)),
      h('div', { class: 'meta' },
        h('span', {}, r.deviceTypeName + (r.deviceModel ? ` · ${r.deviceModel}` : '')),
        h('span', {}, r.city),
        h('span', {}, timeAgo(r.createdAt)),
        r.preferredDate ? h('span', {}, `удобна дата ${new Date(r.preferredDate).toLocaleDateString('bg-BG')}`) : null,
      ),
    ),
    h('div', { class: 'side' },
      h('span', { class: 'offers-chip' }, r.offersCount === 1 ? '1 оферта' : `${r.offersCount} оферти`),
      action,
    ),
  );
}

async function load() {
  const query = new URLSearchParams();
  if (filters.type) query.set('deviceTypeId', filters.type);
  if (filters.city) query.set('city', filters.city);
  if (filters.q) query.set('search', filters.q);

  const [types, all, listings, technicians] = await Promise.all([
    api('/device-types'),
    api('/requests/open'),                       // за броя във всяка категория
    api('/requests/open' + (query.toString() ? `?${query}` : '')),
    api('/technicians'),
  ]);

  // Категории с брой обяви
  const counts = {};
  for (const r of all) counts[r.deviceTypeName] = (counts[r.deviceTypeName] ?? 0) + 1;

  $('#categories').replaceChildren(
    h('a', { class: 'cat' + (filters.type ? '' : ' active'), href: urlWith({ type: null }) },
      'Всички', h('span', { class: 'count' }, String(all.length))),
    ...types.map((t) =>
      h('a', { class: 'cat' + (filters.type === t.deviceTypeId ? ' active' : ''), href: urlWith({ type: t.deviceTypeId }) },
        t.name, h('span', { class: 'count' }, String(counts[t.name] ?? 0)))),
  );

  // Заглавие според филтрите
  const typeName = types.find((t) => t.deviceTypeId === filters.type)?.name;
  const parts = [typeName ?? 'Обяви за IT помощ'];
  if (filters.city) parts.push(`в ${filters.city}`);
  if (filters.q) parts.push(`„${filters.q}“`);
  $('#title').textContent = parts.join(' ');
  $('#count').textContent = listings.length === 1 ? '1 обява' : `${listings.length} обяви`;

  $('#listings').replaceChildren(
    ...(listings.length
      ? listings.map(listingRow)
      : [h('div', { class: 'empty' },
          h('p', {}, 'Няма отворени заявки по тези критерии.'),
          h('a', { class: 'button accent', href: '/new-request.html' }, '+ Публикувай заявка'))]),
  );

  // Най-добри техници
  $('#technicians').replaceChildren(
    ...(technicians.length
      ? technicians.slice(0, 5).map((t) =>
          h('a', { class: 'tech', href: `/technician.html?id=${t.technicianId}` },
            h('div', {}, t.fullName, h('small', {}, `${t.city} · ${t.deviceTypes ?? '—'}`)),
            h('span', { class: 'rating' }, t.avgRating ? `★ ${Number(t.avgRating).toFixed(1)}` : 'нов')))
      : [h('p', { class: 'muted' }, 'Още няма техници.')]),
  );
}

try {
  await load();
} catch (err) {
  showMessage($('#message'), err.message);
}
