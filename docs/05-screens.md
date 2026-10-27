# 5. Екрани

Сайтът е подреден като сайт за обяви: заявките са „обяви за работа“, които техниците разглеждат, а клиентите публикуват с бутона „+ Публикувай заявка“. Front-end-ът е чист HTML, CSS и JavaScript (`fetch`) в `backend/ITspec.Api/wwwroot`. Всички заявки минават през `js/api.js`, който добавя JWT и връща към входа при 401.

| Екран | Файл | Роля | Данни от |
| --- | --- | --- | --- |
| Начало — дъска с обяви (търсачка, категории с брой обяви, списък, най-добри техници) | index.html | всички, без вход | GET /requests/open с филтри, GET /device-types, GET /technicians |
| Вход | login.html | всички | POST /auth/login |
| Регистрация | register.html | всички | POST /auth/register/client или /technician |
| Списък със заявки | requests.html | Client, Technician, Admin | GET /requests (различен според ролята) |
| Нова заявка | new-request.html | Client | GET /device-types, POST /requests |
| Детайли на заявка | request.html?id= | всички роли | GET /requests/{id}, GET /requests/{id}/offers; бутони: accept, cancel, оферта, withdraw, complete, review |
| Моите оферти | my-offers.html | Technician | GET /offers/mine, POST /offers/{id}/withdraw |
| Моите устройства | my-device-types.html | Technician | GET /device-types, GET и PUT /technicians/me/device-types |
| Профил на техник | technician.html?id= | всички | GET /technicians/{id} |
| Админ | admin.html | Admin | /device-types, /admin/users, /admin/stats |

Всеки екран има състояние за зареждане, празен списък и грешка (съобщението от ProblemDetails). Бутоните се показват според ролята, но правата се проверяват на сървъра — скрит бутон не е защита.
