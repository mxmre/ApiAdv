using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace ApiAdv
{
    public class Program
    {
        private static WebApplication? app;
        private static WebApplicationBuilder? builder;
        private static Dictionary<string, List<string>>? advData = null;

        private static Dictionary<string, List<string>> GetAdvDictFromStr(string str)
        {
            var newAdvData = new Dictionary<string, List<string>>();
            foreach (string line in str.Split('\n'))
            {
                string updLine = line.Trim();
                if (string.IsNullOrEmpty(updLine) ||
                    !Regex.IsMatch(updLine, @"^([^\n:]+):((?:/[a-zA-Z0-9]+)+)(?:,(?:/[a-zA-Z0-9]+)+)*$"))
                {
                    continue;
                }

                var parts = updLine.Split(':', 2, StringSplitOptions.None);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var locationsStr = parts[1].Trim();

                var locations = locationsStr.Split(',')
                    .Select(loc => loc.Trim())
                    .Where(loc => !string.IsNullOrEmpty(loc))
                    .ToList();

                newAdvData[key] = locations;
            }
            return newAdvData;
        }

        private static IResult UploadData(IFormFile file)
        {
            app?.Logger.LogInformation("Выполнение PUT /upload...");
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("Файл не выбран или пуст.");
            }

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = reader.ReadToEnd();
                app?.Logger.LogInformation("Содержимое файла: " + content);

                advData = GetAdvDictFromStr(content);
                if (advData == null || advData.Count == 0)
                {
                    app?.Logger.LogError("Ошибка парсинга файла");
                    return Results.BadRequest("Файл имеет неверный формат.");
                }
                return Results.Ok("Файл успешно загружен!");
            }
            catch (Exception ex)
            {
                app?.Logger.LogError(ex, "Ошибка при загрузке файла");
                return Results.StatusCode(500);
            }
        }

        private static bool IsPrefix(string prefix, string target)
        {
            return target.StartsWith(prefix) &&
                   (prefix == target || target[prefix.Length] == '/');
        }

        public static List<string> FindAdvertisers(string targetLocation)
        {
            if (advData == null)
            {
                return new List<string>();
            }

            var result = new List<string>();
            foreach (var entry in advData)
            {
                foreach (var area in entry.Value)
                {
                    if (IsPrefix(area, targetLocation))
                    {
                        result.Add(entry.Key);
                        break;
                    }
                }
            }
            return result;
        }

        private static IResult GetAdvertisers(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return Results.BadRequest("Параметр 'location' обязателен");
            }

            if (!Regex.IsMatch(location, @"^/[a-zA-Z0-9]+(?:/[a-zA-Z0-9]+)*$"))
            {
                return Results.BadRequest("Неверный формат локации. Пример: '/ru/svrd'");
            }

            var advertisers = FindAdvertisers(location);
            app?.Logger.LogInformation("Результат поиска: " + string.Join(", ", advertisers));
            return Results.Ok(advertisers);
        }

        private static IResult GetAntiforgeryToken(IAntiforgery antiforgery, HttpContext context)
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            var antiforgeryOptions = context.RequestServices.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;
            var cookieName = antiforgeryOptions.Cookie.Name ?? "null";
            app?.Logger.LogInformation("Получение токена -> \n\tToken: " + tokens.RequestToken + "\n\tCookieName: " + cookieName + "\n\tCookieValue: " + tokens.CookieToken);
            return Results.Ok(new
            {
                Token = tokens.RequestToken,
                CookieName = cookieName,
                CookieValue = tokens.CookieToken
            });
        }

        public static void Main(string[] args)
        {
            builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 1024 * 1024; // 1MB
            });

            builder.Services.AddAntiforgery();

            app = builder.Build();

            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (AntiforgeryValidationException ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(
                        "Антифоржерный токен отсутствует или недействителен. " +
                        "Пожалуйста, получите токен через /get-token и включите его в запрос.");
                    app.Logger.LogWarning(ex, "Ошибка валидации антифоржерного токена");
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Произошла ошибка на сервере: " + ex.Message);
                    app.Logger.LogError(ex, "Необработанное исключение");
                }
            });

            app.UseRouting();
            app.UseAntiforgery();

            app.MapGet("/", () => "Используйте /upload для загрузки файла (не забудьте получить токен через /get-token) и /search для поиска рекламодателей.");
            app.MapPut("/upload", UploadData);
            app.MapGet("/search", GetAdvertisers);
            app.MapGet("/get-token", GetAntiforgeryToken);

            app.Run();
        }
    }
}