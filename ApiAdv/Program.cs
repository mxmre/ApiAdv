using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.RegularExpressions;

namespace ApiAdv
{
    public class Program
    {
        private static WebApplication? app;
        private static Dictionary<string, List<string>>? advData = null;
        private static Dictionary<string, List<string>>? GetAdvDictFromStr(string str)
        {
            Dictionary<string, List<string>>? newAdvData = new();
            string[] lines = str.Split('\n');
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line.Trim()) ||
                    !Regex.IsMatch(line.Trim(), 
                        @"^([^\n:]+):((?:/[a-zA-Z0-9]+)+)(?:,(?:/[a-zA-Z0-9]+)+)*$") )
                    return null;
                string[] parts = line.Split( ':' , 2, StringSplitOptions.None);
                if (parts.Length != 2) return null;
                string key = parts[0].Trim(); 
                string locationsStr = parts[1].Trim();

                List<string> locations = locationsStr.Split(',')
                    .Select(loc => loc.Trim())
                    .Where(loc => !string.IsNullOrEmpty(loc))
                    .ToList();
                newAdvData.Add(key, locations);
            }
            return newAdvData;
        }//(/[a-zA-Z0-9]+)+
        private static IResult UploadData(IFormFile file)
        {
            app?.Logger.LogInformation("Выполнение PUT /upload...");
            if (file == null || file.Length == 0)
            {
                app?.Logger.LogError("Ошибка чтения файла!");
                return Results.BadRequest("Файл не выбран или пуст.");
            }

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                app?.Logger.LogInformation("Чтение файла...");
                string content = reader.ReadToEndAsync().Result;
                
                advData = GetAdvDictFromStr(content);
                if (advData is null)
                {
                    app?.Logger.LogWarning("advData is null");
                }
                app?.Logger.LogInformation("Чтение файла завершено!");
            }
            app?.Logger.LogInformation("Выполнение PUT /upload завершено!");
            return Results.Ok("Файл успешно загружен!");
        }



        private static bool IsPrefix(string prefix, string target)
        {
            if (!target.StartsWith(prefix))
                return false;
            return prefix == target || target[prefix.Length] == '/';
        }
        public static List<string>? FindAdvertisers(string targetLocation)
        {
            var result = new List<string>();
            if (advData is null)
                return null;
            foreach (var entry in advData)
            {
                string advertiserName = entry.Key;
                List<string> areas = entry.Value;

                foreach (string area in areas)
                {
                    if (IsPrefix(area, targetLocation))
                    {
                        result.Add(advertiserName);
                        break; // Не проверяем остальные локации этой площадки
                    }
                }
            }

            return result;
        }
        private static IResult GetAdvertisers(string location)
        {
            app?.Logger.LogInformation("Выполнение GET /search...");
            if (string.IsNullOrEmpty(location))
            {
                app?.Logger.LogError("Не указан параметр 'location'!");
                return Results.BadRequest("Параметр 'location' обязателен");
            }

            if (!Regex.IsMatch(location, @"(/[a-zA-Z0-9]+)+"))
            {
                app?.Logger.LogError("Неверный формат локации!");
                return Results.BadRequest("Неверный формат локации. Пример: '/ru/svrd'");
            }
            var advertisers = FindAdvertisers(location);
            app?.Logger.LogInformation("Выполнение GET /search завершено!");
            if(advertisers is null)
            {
                app?.Logger.LogWarning("advertisers is null");
            }
            return Results.Ok(advertisers);
        }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 1024 * 1024; // 1MB
            });
            app = builder.Build();
            app.MapGet("/", () => "/upload для загрузки файла с настройками, /search для поиска рекламодателей.");
            app.MapPut("/upload", UploadData);
            app.MapGet("/search", GetAdvertisers);

            app.Run();
        }
    }
}
