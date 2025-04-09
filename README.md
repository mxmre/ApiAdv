# ApiAdv

ApiAdv — это простое веб-приложение на ASP.NET Core 8.0 для загрузки данных о рекламодателях и поиска рекламодателей по заданной локации. Приложение поддерживает загрузку файлов через PUT-запросы и требует антифоржерный токен для защиты от CSRF-атак. Приложение будет доступно по адресу http://localhost:5000.

### Требования

- .NET 8.0 SDK
- Windows, Linux или macOS
- Инструмент для выполнения HTTP-запросов (например, `curl`)

### Установка

1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/mxmre/ApiAdv.git
   cd ApiAdv
   ```
# Использование API
## Получение антифоржерного токена
**Запрос:**
```bash
curl http://localhost:5000/get-token
```
**Ответ:**
```json
{
  "token": "CfDJ8NmeLBhYwvFHrrJSMeI_F0fWEQlL...",
  "cookieName": ".AspNetCore.Antiforgery.<что-то>",
  "cookieValue": "CfDJ8NmeLBhYwvFHrrJSMeI_F0fa3tJo..."
}
```
## Загрузка файла
**Формат файла:**
```txt
Advertiser1:/location1,/location2
Advertiser2:/location3
```
Каждая строка должна содержать имя рекламодателя, двоеточие и список локаций, разделённых запятыми.

**Пример запроса:**
```bash
curl -X PUT -F "file=@путь_к_файлу.txt" ^
     -F "__RequestVerificationToken=<token>" ^
     -b "<cookieName>=<cookieValue>" ^
     http://localhost:5000/upload
```
**Ответ:**
```txt
"Файл успешно загружен!"
```
## Поиск рекламодателей
**Пример запроса:**
```bash
curl "http://localhost:5000/search?location=/ru/svrd"
```
**Ответ:**
```json
["Advertiser1", "Advertiser2"]
```


