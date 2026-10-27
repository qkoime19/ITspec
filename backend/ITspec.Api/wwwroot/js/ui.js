// Общи помощни функции за страниците: създаване на елементи, менюто, формати.
import { getUser, logout } from './api.js';

// h('div', { class: 'card' }, 'текст', child) — създава елемент.
// Текстът винаги се слага като textContent, никога като HTML (защита от XSS).
export function h(tag, attrs = {}, ...children) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs ?? {})) {
    if (value === undefined || value === null || value === false) continue;
    if (key.startsWith('on') && typeof value === 'function') {
      el.addEventListener(key.slice(2).toLowerCase(), value);
    } else if (key === 'class') {
      el.className = value;
    } else if (value === true) {
      el.setAttribute(key, '');
    } else {
      el.setAttribute(key, value);
    }
  }
  for (const child of children.flat()) {
    if (child === undefined || child === null || child === false) continue;
    el.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return el;
}

export const $ = (selector) => document.querySelector(selector);

export const REQUEST_STATUS = {
  Open: 'Отворена',
  Assigned: 'Възложена',
  Completed: 'Завършена',
  Cancelled: 'Прекратена',
};

export const OFFER_STATUS = {
  Pending: 'Чака',
  Accepted: 'Приета',
  Rejected: 'Отхвърлена',
  Withdrawn: 'Оттеглена',
};

export const ROLE_NAME = {
  Client: 'Клиент',
  Technician: 'Техник',
  Admin: 'Администратор',
};

export function badge(status, labels = REQUEST_STATUS) {
  return h('span', { class: `badge badge-${status.toLowerCase()}` }, labels[status] ?? status);
}

export function formatDate(value) {
  if (!value) return '—';
  return new Date(value).toLocaleDateString('bg-BG');
}

export function formatPrice(value) {
  return `${Number(value).toFixed(2)} €`;
}

export function stars(rating) {
  if (rating === null || rating === undefined) return 'няма оценки';
  const full = Math.round(Number(rating));
  return '★'.repeat(full) + '☆'.repeat(5 - full) + ` ${Number(rating).toFixed(1)}`;
}

export function showMessage(container, text, type = 'error') {
  container.replaceChildren(h('div', { class: `alert alert-${type}`, role: 'alert' }, text));
}

export function clearMessage(container) {
  container.replaceChildren();
}

// Горната лента като на сайт за обяви: лого, меню според ролята, бутон „Публикувай заявка“
// и търсачка, която води към началната страница с филтрите.
export function renderNav({ search = true } = {}) {
  const user = getUser();
  const links = [];

  if (!user) {
    links.push(h('a', { href: '/login.html' }, 'Вход'), h('a', { href: '/register.html' }, 'Регистрация'));
  } else {
    if (user.role === 'Client') {
      links.push(h('a', { href: '/requests.html' }, 'Моите заявки'));
    }
    if (user.role === 'Technician') {
      links.push(
        h('a', { href: '/requests.html' }, 'За моите устройства'),
        h('a', { href: '/my-offers.html' }, 'Моите оферти'),
        h('a', { href: '/my-device-types.html' }, 'С какво работя'),
      );
    }
    if (user.role === 'Admin') {
      links.push(h('a', { href: '/requests.html' }, 'Всички заявки'), h('a', { href: '/admin.html' }, 'Админ'));
    }
    links.push(
      h('span', { class: 'who' }, `${user.fullName} · ${ROLE_NAME[user.role] ?? user.role}`),
      h('button', { class: 'link', onclick: logout }, 'Изход'),
    );
  }

  // „Публикувай заявка“ е за клиенти и за посетители (те минават през входа).
  if (!user || user.role === 'Client') {
    links.push(h('a', { href: '/new-request.html', class: 'post-btn' }, '+ Публикувай заявка'));
  }

  const topbar = h('div', { class: 'topbar' },
    h('div', { class: 'topbar-inner' },
      h('a', { href: '/index.html', class: 'logo' },
        h('b', {}, 'IT', h('span', {}, 'spec')),
        h('small', {}, 'обяви за помощ с компютри и телефони')),
      h('nav', { class: 'topnav', 'aria-label': 'Главно меню' }, links)));

  const parts = [topbar];

  if (search) {
    const params = new URLSearchParams(location.search);
    const isHome = location.pathname === '/' || location.pathname.endsWith('/index.html');
    const form = h('form', { action: '/index.html', method: 'get', role: 'search' },
      h('input', { name: 'q', placeholder: 'Какво трябва да се поправи? напр. екран, батерия, Wi-Fi',
                   'aria-label': 'Търсене', value: isHome ? params.get('q') ?? '' : '' }),
      h('input', { name: 'city', placeholder: 'Град', 'aria-label': 'Град', value: isHome ? params.get('city') ?? '' : '' }),
      h('button', { type: 'submit' }, 'Търси'));
    if (isHome && params.get('type')) {
      form.append(h('input', { type: 'hidden', name: 'type', value: params.get('type') }));
    }
    parts.push(h('div', { class: 'searchbar' }, form));
  }

  document.querySelector('#nav')?.replaceChildren(...parts);

  if (!document.querySelector('.footer')) {
    document.body.append(h('footer', { class: 'footer' }, 'ITspec · учебен проект · тема 8 „IT помощ — компютри и телефони“'));
  }
}

// Малка картинка за вида устройство (SVG, без външни файлове).
const ICONS = {
  'Лаптоп': '<path d="M4 6h16v10H4z M2 18h20" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/>',
  'Настолен компютър': '<path d="M3 4h18v12H3z M9 20h6 M12 16v4" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/>',
  'Смартфон': '<path d="M7 2h10v20H7z M11 18h2" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/>',
  'Таблет': '<path d="M4 3h16v18H4z M11 18h2" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/>',
  'Принтер': '<path d="M6 9V3h12v6 M4 9h16v8H4z M7 14h10v7H7z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/>',
  'Рутер и мрежа': '<path d="M3 14h18v6H3z M7 17h.01 M12 10a4 4 0 0 1 4 4 M8 14a4 4 0 0 1 4-4 M12 6a8 8 0 0 1 8 8 M4 14a8 8 0 0 1 8-8" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>',
};

export function deviceIcon(typeName) {
  const box = h('div', { class: 'thumb', 'aria-hidden': 'true' });
  if (ICONS[typeName]) {
    box.innerHTML = `<svg viewBox="0 0 24 24">${ICONS[typeName]}</svg>`;   // константи, не потребителски текст
  } else {
    box.textContent = (typeName || '?').charAt(0).toUpperCase();
  }
  return box;
}

export function timeAgo(value) {
  const days = Math.floor((Date.now() - new Date(value).getTime()) / 86400000);
  if (days <= 0) return 'днес';
  if (days === 1) return 'вчера';
  if (days < 30) return `преди ${days} дни`;
  return formatDate(value);
}

// Страница само за дадени роли. Без вход -> към входа; грешна роля -> началото.
export function requireRole(...roles) {
  const user = getUser();
  if (!user) {
    location.href = '/login.html?next=' + encodeURIComponent(location.pathname + location.search);
    throw new Error('not logged in');
  }
  if (roles.length && !roles.includes(user.role)) {
    location.href = '/index.html';
    throw new Error('wrong role');
  }
  return user;
}
