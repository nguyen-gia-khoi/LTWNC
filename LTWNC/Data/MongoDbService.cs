using MongoDB.Driver;

namespace LTWNC.Data
{
    public class MongoDbService
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoDatabase? _database;
        public MongoDbService(IConfiguration configuration) 
        {
                _configuration = configuration;

            var connectionString = _configuration.GetConnectionString("DBconnection");
            var mongoUrl = MongoUrl.Create(connectionString);
            var mongoClient = new MongoClient(mongoUrl);
            _database = mongoClient.GetDatabase("LTWNC");
        }

        public IMongoDatabase? Database => _database;
    }
}
