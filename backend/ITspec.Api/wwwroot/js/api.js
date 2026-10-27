// Общ файл за всички заявки към API-то.
// Добавя JWT в заглавката Authorization и обработва 401 и 403.

const BASE = '/api/v1';
const TOKEN_KEY = 'itspec_token';
const USER_KEY = 'itspec_user';

export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.status = status;
  }
}

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

// Потребителят (id, име, роля) идва от отговора на /auth/login.
// Ролята тук служи САМО за показване на правилните бутони — правата се проверяват на сървъра.
export function getUser() {
  try {
    return JSON.parse(localStorage.getItem(USER_KEY));
  } catch {
    return null;
  }
}

export function saveLogin(loginResponse) {
  localStorage.setItem(TOKEN_KEY, loginResponse.token);
  localStorage.setItem(USER_KEY, JSON.stringify(loginResponse.user));
}

export function logout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  location.href = '/login.html';
}

function errorMessage(data, status) {
  if (data?.errors) {
    // Грешки от валидацията на DTO: { errors: { Field: ["..."] } }
    return Object.values(data.errors).flat().join(' ');
  }
  if (data?.title) return data.title;
  if (status === 403) return 'Нямате право за това действие.';
  if (status === 404) return 'Не е намерено.';
  return 'Възникна грешка. Опитайте отново.';
}

export async function api(path, { method = 'GET', body } = {}) {
  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(BASE + path, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  // 401: няма валиден токен (изтекъл или липсва) -> обратно към входа.
  if (response.status === 401 && !path.startsWith('/auth/login')) {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    location.href = '/login.html?next=' + encodeURIComponent(location.pathname + location.search);
    throw new ApiError(401, 'Влезте отново.');
  }

  const data = response.status === 204 ? null : await response.json().catch(() => null);

  // 403 и всички други грешки: съобщението от ProblemDetails се показва на страницата.
  if (!response.ok) {
    throw new ApiError(response.status, errorMessage(data, response.status));
  }

  return data;
}
