# 4. API договор

Всички адреси започват с `/api/v1`. Текущият потребител винаги идва от JWT токена, никога от тялото или адреса. Грешките са във формат ProblemDetails.

| Метод и адрес | Роля | Успех | Възможни грешки |
| --- | --- | --- | --- |
| POST /auth/register/client | всички | 201 | 400, 409 (зает имейл) |
| POST /auth/register/technician | всички | 201 | 400, 409 |
| POST /auth/login | всички | 200 + токен | 400, 401, 403 (блокиран) |
| GET /auth/me | логнат | 200 | 401, 403 |
| GET /device-types | всички | 200 | — |
| POST /device-types | Admin | 201 | 400, 401, 403, 409 |
| PUT /device-types/{id} | Admin | 204 | 400, 401, 403, 404, 409 |
| DELETE /device-types/{id} | Admin | 204 (IsActive = false) | 401, 403, 404 |
| GET /requests | логнат | 200 (според ролята) | 401 |
| GET /requests/{id} | собственик / техник с оферта или от вида / Admin | 200 | 401, 403, 404 |
| POST /requests | Client | 201 + Location | 400, 401, 403 |
| POST /requests/{id}/cancel | Client (собственик) | 204 | 401, 403, 404, 409 |
| POST /requests/{id}/complete | Technician (приетата оферта) | 204 | 401, 403, 404, 409 |
| POST /requests/{id}/review | Client (собственик) | 201 | 400, 401, 403, 409 |
| GET /requests/{id}/offers | Client (собственик), Admin | 200 | 401, 403, 404 |
| POST /requests/{id}/offers | Technician | 201 | 400, 401, 403 (чужд вид), 404, 409 (не е Open / вече има оферта) |
| GET /offers/mine | Technician | 200 | 401, 403 |
| POST /offers/{id}/accept | Client (собственик) | 200 | 401, 403, 409 |
| POST /offers/{id}/withdraw | Technician (автор) | 204 | 401, 403, 404, 409 |
| GET /technicians | всички | 200 | — |
| GET /technicians/{id} | всички | 200 + отзиви | 404 |
| GET /technicians/me/device-types | Technician | 200 | 401, 403 |
| PUT /technicians/me/device-types | Technician | 204 | 401, 403, 409 |
| GET /admin/users | Admin | 200 | 401, 403 |
| POST /admin/users/{id}/block, /unblock | Admin | 204 | 401, 403, 404, 409 |
| GET /admin/stats | Admin | 200 | 401, 403 |

## Примери

**Вход**

```http
POST /api/v1/auth/login
{ "email": "maria@itspec.local", "password": "Demo123!" }
```
```json
{ "token": "eyJ...", "expiresAt": "2026-10-20T14:00:00Z",
  "user": { "userId": 2, "fullName": "Мария Иванова", "email": "maria@itspec.local", "city": "Враца", "role": "Client" } }
```

**Нова заявка**

```http
POST /api/v1/requests
Authorization: Bearer eyJ...
{ "deviceTypeId": 1, "title": "Лаптопът не зарежда", "description": "Зарядното свети, но батерията стои на 0%.", "deviceModel": "Acer Aspire 5" }
```
→ `201 Created`, `Location: /api/v1/requests/7`, тялото е `RequestDetailsDto`.

**Приемане на оферта**

```http
POST /api/v1/offers/12/accept
Authorization: Bearer eyJ...
```
→ `200 { "offerId": 12, "supportRequestId": 7, "requestStatus": "Assigned" }`
→ `409 { "title": "Техникът вече има 3 поети заявки и не може да поеме нова. Изберете друга оферта.", "status": 409 }`
