using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public enum FederatedSourceType
{
    SqlServer = 1,
    PostgreSql = 2,
    MySql = 3,
    Oracle = 4,
    MongoDb = 5,
    CosmosDb = 6,
    ElasticSearch = 7,
    Redis = 8,
    Api = 9,
    VectorStore = 10,
    File = 11
}
