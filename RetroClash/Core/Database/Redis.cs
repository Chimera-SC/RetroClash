using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RetroClash.Logic;
using RetroClash.Logic.Manager;
using StackExchange.Redis;

namespace RetroClash.Core.Database
{
    public class Redis
    {
        private static IDatabase _playerProfiles;
        private static IDatabase _playerObjects;
        private static IDatabase _alliances;
        private static IServer _server;

        private static ConnectionMultiplexer _connection;

        public static JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None
        };

        public Redis()
        {
            try
            {
                var config = new ConfigurationOptions
                {
                    AllowAdmin = true,
                    ConnectTimeout = 10000,
                    ConnectRetry = 10,
                    HighPrioritySocketThreads = true,
                    Password = Resources.Configuration.RedisPassword
                };

                config.EndPoints.Add(Resources.Configuration.RedisServer, 6379);

                _connection = ConnectionMultiplexer.Connect(config);

                _playerProfiles = _connection.GetDatabase(0);
                _playerObjects = _connection.GetDatabase(1);
                _alliances = _connection.GetDatabase(2);
                _server = _connection.GetServer(Resources.Configuration.RedisServer, 6379);
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public static bool IsConnected => _server != null;

        public static async Task CachePlayer(Player player)
        {
            try
            {
                if (player == null)
                    return;

                await _playerProfiles.StringSetAsync(player.AccountId.ToString(),
                    JsonConvert.SerializeObject(player, Settings), TimeSpan.FromHours(4));

                if (player.LogicGameObjectManager != null)
                    await _playerObjects.StringSetAsync(player.AccountId.ToString(), player.LogicGameObjectManager.Json,
                        TimeSpan.FromHours(4));
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public static async Task UncachePlayer(long id)
        {
            try
            {
                await _playerProfiles.KeyDeleteAsync(id.ToString());
                await _playerObjects.KeyDeleteAsync(id.ToString());
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public static async Task CacheAlliance(Alliance alliance)
        {
            try
            {
                await _alliances.StringSetAsync(alliance.Id.ToString(),
                    JsonConvert.SerializeObject(alliance, Settings), TimeSpan.FromHours(4));
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public static async Task UncacheAlliance(long id)
        {
            try
            {
                await _alliances.KeyDeleteAsync(id.ToString());
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }
        }

        public static async Task<Player> GetCachedPlayer(long id)
        {
            try
            {
                var json = await _playerProfiles.StringGetAsync(id.ToString());
                if (string.IsNullOrEmpty(json))
                    return null;

                var player = JsonConvert.DeserializeObject<Player>(json, Settings);
                if (player == null)
                    return null;

                var objects = await _playerObjects.StringGetAsync(id.ToString());
                if (!string.IsNullOrEmpty(objects))
                {
                    player.LogicGameObjectManager =
                        JsonConvert.DeserializeObject<LogicGameObjectManager>(objects, Settings);
                }

                return player;
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);
            }

            return null;
        }

        public static async Task<Player> GetRandomCachedPlayer()
        {
            return await GetCachedPlayer(Convert.ToInt64((await _playerProfiles.KeyRandomAsync()).ToString()));
        }

        public static async Task<Alliance> GetCachedAlliance(long id)
        {
            try
            {
                var json = await _alliances.StringGetAsync(id.ToString());
                if (string.IsNullOrEmpty(json))
                    return null;

                return JsonConvert.DeserializeObject<Alliance>(json, Settings);
            }
            catch (Exception exception)
            {
                Logger.Log(exception, Enums.LogType.Error);

                return null;
            }
        }

        public static int CachedPlayers()
        {
            try
            {
                return Convert.ToInt32(
                    _connection.GetServer(Resources.Configuration.RedisServer, 6379).Info("keyspace")[0]
                        .ElementAt(_playerProfiles.Database)
                        .Value
                        .Split(new[] {"keys="}, StringSplitOptions.None)[1]
                        .Split(new[] {",expires="}, StringSplitOptions.None)[0]);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}