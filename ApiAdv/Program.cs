using Microsoft.AspNetCore.Http.Features;
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
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("Файл не выбран или пуст.");
            }

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                string content = reader.ReadToEndAsync().Result;
                app?.Logger.LogInformation($"Содержимое файла");
                advData = GetAdvDictFromStr(content);
            }
            return Results.Ok("Файл успешно загружен!");
        }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 1024 * 1024; // 1MB
            });
            app = builder.Build();

            app.MapPost("/upload", UploadData);

            app.Run();
        }
    }
}
