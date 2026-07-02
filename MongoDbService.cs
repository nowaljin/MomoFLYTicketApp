using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Authentication;

namespace AirlineTicketReservationSystem;

/// <summary>
/// Singleton MongoDB service managing connections and database access.
/// Reuses a single MongoClient instance across the application.
/// </summary>
public class MongoDbService : IDisposable
{
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private static MongoDbService? _instance;
    private static readonly object _lockObject = new();

    public const string DefaultDatabaseName = "airline_reservation_db";

    private MongoDbService(string connectionUri, string databaseName = DefaultDatabaseName)
    {
        // Configure client settings with ServerApi for compatibility
        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        settings.UseTls = true;
        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12,
            CheckCertificateRevocation = false
        };

        // Configure connection pooling for desktop application
        // Desktop apps are long-running with stable concurrency
        settings.MaxConnectionPoolSize = 50;
        settings.MinConnectionPoolSize = 10;

        _client = new MongoClient(settings);
        _database = _client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Gets or creates the singleton MongoDB service instance.
    /// </summary>
    public static MongoDbService GetInstance(string connectionUri, string databaseName = DefaultDatabaseName)
    {
        if (_instance == null)
        {
            lock (_lockObject)
            {
                _instance ??= new MongoDbService(connectionUri, databaseName);
            }
        }
        return _instance;
    }

    /// <summary>
    /// Resets the singleton instance. Useful for testing or changing connections.
    /// </summary>
    public static void ResetInstance()
    {
        lock (_lockObject)
        {
            _instance?.Dispose();
            _instance = null;
        }
    }

    /// <summary>
    /// Tests the connection to MongoDB by pinging the server.
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var adminDb = _client.GetDatabase("admin");
            var result = await adminDb.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Tests the connection synchronously (for UI blocking operations).
    /// </summary>
    public bool TestConnection()
    {
        try
        {
            var adminDb = _client.GetDatabase("admin");
            adminDb.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the MongoDB database instance.
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// Gets a collection by name.
    /// </summary>
    public IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        return _database.GetCollection<BsonDocument>(collectionName);
    }

    /// <summary>
    /// Disposes the MongoClient and releases resources.
    /// </summary>
    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
