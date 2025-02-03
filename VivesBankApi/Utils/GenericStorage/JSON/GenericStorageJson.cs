using System.Reactive.Linq;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace VivesBankApi.Utils.GenericStorage.JSON;

/// <summary>
/// Provides generic storage functionality for importing and exporting JSON data.
/// </summary>
/// <typeparam name="T">The type of objects being stored and retrieved.</typeparam>
public class GenericStorageJson<T> : IGenericStorageJson<T> where T : class
{
    protected readonly ILogger<GenericStorageJson<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericStorageJson{T}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging messages.</param>
    public GenericStorageJson(ILogger<GenericStorageJson<T>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Imports objects of type <typeparamref name="T"/> from an uploaded JSON file as an observable stream.
    /// </summary>
    /// <param name="fileStream">The uploaded JSON file containing the data.</param>
    /// <returns>An <see cref="IObservable{T}"/> stream of deserialized objects.</returns>
    public IObservable<T> Import(IFormFile fileStream)
    {
        _logger.LogInformation($"Importing {typeof(T).Name} from a JSON file");
        return Observable.Create<T>(async (observer, cancellationToken) =>
        {
            try
            {
                using var stream = fileStream.OpenReadStream();
                using var streamReader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(streamReader)
                {
                    SupportMultipleContent = true
                };

                var serializer = new JsonSerializer
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                };

                while (await jsonReader.ReadAsync(cancellationToken))
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        var obj = serializer.Deserialize<T>(jsonReader);
                        observer.OnNext(obj);
                    }
                }
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        });
    }

    /// <summary>
    /// Exports a list of objects of type <typeparamref name="T"/> to a JSON file and returns a file stream.
    /// </summary>
    /// <param name="entities">The list of objects to be exported.</param>
    /// <returns>A <see cref="FileStream"/> containing the exported JSON file.</returns>
    public async Task<FileStream> Export(List<T> entities)
    {
        _logger.LogInformation($"Exporting {typeof(T).Name} to a JSON file");

        var json = JsonConvert.SerializeObject(entities, Formatting.Indented);
        var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "Json");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var fileName = $"{typeof(T).Name}sInSystem-" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json";
        var filePath = Path.Combine(directoryPath, fileName);

        await File.WriteAllTextAsync(filePath, json);

        _logger.LogInformation($"File written to: {filePath}");

        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <summary>
    /// Imports a list of objects of type <typeparamref name="T"/> from a specified JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>A list of deserialized objects of type <typeparamref name="T"/>.</returns>
    public async Task<List<T>> ImportFromFile(string filePath)
    {
        _logger.LogInformation($"Importing {typeof(T).Name} from file: {filePath}");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return new List<T>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
    }
}