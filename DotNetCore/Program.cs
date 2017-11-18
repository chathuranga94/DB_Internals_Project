using System;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using FluentAssertions;

using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using System.Text;

namespace DotNetCore
{
    class Program
    {
        private static IMongoClient _dbClient;
        private static IMongoDatabase _db;
        private static IMongoCollection<BsonDocument> _dbUser;

        private static IMongoClient _cacheClient;
        private static IMongoDatabase _cache;
        private static IMongoCollection<BsonDocument> _cacheUser;

        private static RedisCache _redis_cache;

        public static int CACHE_Access;
        public static int DB_Access;

        public static int MODE;
        public const int NO_CACHE = 0;
        public const int MONGO_CACHE = 1;
        public const int REDIS_CACHE = 2;
        public static int USER_COUNT;
        static void Main(string[] args)
        {

            _dbClient = new MongoClient("mongodb://localhost:27020");
            _db = _dbClient.GetDatabase("userdb1");
            _dbUser = _db.GetCollection<BsonDocument>("db_test1");

            _cacheClient = new MongoClient("mongodb://localhost:27018");
            _cache = _cacheClient.GetDatabase("userdb1");
            _cacheUser = _cache.GetCollection<BsonDocument>("db_test2");

            _redis_cache = new RedisCache(new RedisCacheOptions
            { Configuration = "localhost", InstanceName = "db_test4112" }
            );

            /*******************************************************************/
            RunTestCases();


            /*******************************************************************/

            
            Random rnd = new Random();
            int getIndexes = rnd.Next(1, 100);
            //int setIndexes = rnd.Next(1, 1000); 

            List<int> getId_List = new List<int>();

            for (int times = 0; times < 100000; times++)
            {
                int random = rnd.Next(1, 100);
                getId_List.Add(random);
            }

            // Stopwatch stopwatch_add = new Stopwatch();
            // stopwatch_add.Start();
            // for (int id = 1; id <= 1000; id++)
            // {
            //     var new_doc = new BsonDocument
            //     {
            //         { "name", "bibi"+id },
            //         { "user_id", id }
            //     };
            //     AddUser(new_doc);
            // }
            // stopwatch_add.Stop();
            // Console.WriteLine("ADD USERS CONTINOUSLY FOR ID 0 to 1000 TIME: {0}", stopwatch_add.Elapsed);

            DB_Access = 0; CACHE_Access = 0;
    	    Stopwatch stopwatch_get = new Stopwatch();
            stopwatch_get.Start();
            foreach (int id in getId_List)
            {
                GetUser_RedisCache(id);
                // GetUser_NoCache(id);
                // GetUser_WithCache(id);
                //GetUserAsync_WithCache(id);
            }
            stopwatch_get.Stop();
            Console.WriteLine("\nTIME => {0}", stopwatch_get.Elapsed);
            Console.WriteLine("IN DB: " + DB_Access + "  IN CACHE: " + CACHE_Access);
            
        }

        private static void RunTestCases()
        {
            int[] testLengthArr = { 100, 1000, 10000, 100000 };
            float[,] ratiosArr = { { 0.04f, 0.02f, 0.02f }, { 0.4f, 0.2f, 0.2f }, { 0.9f, 0.04f, 0.04f }, { 0.25f, 0.25f, 0.25f } };
            for (int length = 0; length < testLengthArr.Length; length++)
            {
                int N = testLengthArr[length];
                USER_COUNT = N / 10;
                for (int ratioIndex = 0; ratioIndex <4; ratioIndex++)
                {
                    for (MODE = 0; MODE < 2;MODE++)
                    {
                        _dbUser = _db.GetCollection<BsonDocument>("db_test");
                        _cacheUser = _cache.GetCollection<BsonDocument>("db_test");
                        Console.WriteLine("Test Case: N={0}, MODE: {1}", N, (MODE == NO_CACHE ? "NO CACHE" : MODE == MONGO_CACHE ? "MONGO DB IN MEMMORY CACHE" : "REDIS CACHE"));
                        Console.WriteLine("Insert ratio: {0}, Update ratio: {1}, Delete ratio: {2}", ratiosArr[ratioIndex, 0], ratiosArr[ratioIndex, 1], ratiosArr[ratioIndex, 2]);
                        for (int id = 1; id <= USER_COUNT; id++)
                        {
                            var new_doc = new BsonDocument
                            {
                                { "name", "user"+id },
                                { "user_id", id }
                            };
                            AddUser(new_doc);
                        }
                        RunTestCase(N, ratiosArr[ratioIndex, 0], ratiosArr[ratioIndex, 1], ratiosArr[ratioIndex, 2]);
                        Console.WriteLine("----------------------------------------------------------------------------------------------------------");
                        _db.DropCollection("db_test");
                        _cache.DropCollection("db_test");
                    }

                }
            }
        }

        private static void RunTestCase(int N, float insertRatio, float updateRatio, float deleteRatio)
        {
            Tuple<int[], int[]> test = CreateOperationsList(N, insertRatio, updateRatio, deleteRatio);
            int[] opArray = test.Item1;
            int[] idxArray = test.Item2;

            Stopwatch stopWatch = new Stopwatch();
            int[] count = new int[4];

            Console.WriteLine("Running {0} records randomly", N);
            CACHE_Access = 0;
            DB_Access = 0;

            for (int index = 0; index < opArray.Length; index++)
            {

                if (opArray[index] == 0)
                {
                    var new_user_document = new BsonDocument { { "name", "user" + idxArray[index] }, { "user_id", idxArray[index] } };
                    stopWatch.Start();
                    AddUser(new_user_document);
                    stopWatch.Stop();
                }
                else if (opArray[index] == 1)
                {
                    stopWatch.Start();
                    UpdateUser(idxArray[index]);
                    stopWatch.Stop();
                }
                else if (opArray[index] == 2)
                {
                    stopWatch.Start();
                    DeleteUser(idxArray[index]);
                    stopWatch.Stop();
                }
                else if (opArray[index] == 3)
                {
                    stopWatch.Start();
                    GetUser(idxArray[index]);
                    stopWatch.Stop();
                }

                count[opArray[index]]++;
            }

            TimeSpan fullTime = stopWatch.Elapsed;

            Console.WriteLine("FULLTIME: " + fullTime.TotalMilliseconds + "ms");
            Console.WriteLine("Insert:{0}  Update:{1}  Delete:{2}  Find:{3}", count[0], count[1], count[2], count[3]);
            Console.WriteLine("IN DB: " + DB_Access + "  IN CACHE: " + CACHE_Access);
        }

        public static BsonDocument GetUser_RedisCache(int user_id)
        {
            var _InREDIS = _redis_cache.Get(user_id.ToString());
            if (_InREDIS != null)
            {
                CACHE_Access++;
                // return Encoding.UTF8.GetString(_InREDIS).ToBsonDocument();
                return new BsonDocument();
            }
            else
            {
                DB_Access++;
                var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                var db_query = _dbUser.Find(filter);
                if ((int)db_query.Count() > 0)
                {
                    String db_user_str = db_query.First().ToString();
                    _redis_cache.Set(user_id.ToString(), Encoding.UTF8.GetBytes(db_user_str), new DistributedCacheEntryOptions());
                    // return db_user_str.ToBsonDocument();
                    return new BsonDocument();
                }
                else
                {
                    return new BsonDocument();
                }
            }
        }

        public static BsonDocument GetUser_WithCache(int user_id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var cache_query = _cacheUser.Find(filter);

            if ((int)cache_query.Count() > 0)
            {
                CACHE_Access++;
                return cache_query.First();  //.ToList() and .Count
            }
            else
            {
                DB_Access++;
                var db_query = _dbUser.Find(filter);
                if ((int)db_query.Count() > 0)
                {
                    _cacheUser.InsertOne(db_query.First());
                    return db_query.First();

                    /* Is ObjectId set too?

                    var queried_user = (User)db_query.First(); 
                    var new_user_doc = new BsonDocument
                    {
                        { "name", queried_user.user_name },
                        { "user_id", queried_user.user_id }
                    };
                    _cacheUser.InsertOne(new_user_doc);
                    */
                }
                else
                {
                    return new BsonDocument();
                }
            }
        }

        public static BsonDocument GetUser_NoCache(int user_id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var db_query = _dbUser.Find(filter);

            DB_Access++;
            if ((int)db_query.Count() > 0)
            {
                return db_query.First();
            }
            else
            {
                return new BsonDocument();
            }
        }

        public static async Task<BsonDocument> GetUserAsync_WithCache(int user_id)
        {

            Console.WriteLine("\nASYNC CALL: " + user_id);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var cache_query = await _cacheUser.FindAsync(filter);   // FindAsync vs Find(filter).ToListAsync();  // ??? Find(filter).FirstAsync()

            if ((int)cache_query.ToList().Count > 0)
            {
                CACHE_Access++;

                Console.WriteLine("IN CACHE: ASYNC CALL ENDS..." + user_id);
                return cache_query.First();
            }
            else
            {
                var db_query = await _dbUser.FindAsync(filter);
                if ((int)db_query.ToList().Count > 0)
                {
                    DB_Access++;
                    _cacheUser.InsertOne(db_query.First());
                    Console.WriteLine("IN DB: ASYNC CALL ENDS..." + user_id);
                    return db_query.First();
                }
                else
                {
                    return new BsonDocument();
                }
            }
        }

        public static Boolean AddUser(BsonDocument new_user)
        {
            DB_Access++;
            _dbUser.InsertOne(new_user);

            return true;
        }

        public static BsonDocument UpdateUser_RedisCache(int user_id)
        {

            String timestamp = GetTimestamp(DateTime.Now);
            var _InREDIS = _redis_cache.Get(user_id.ToString());
            if (_InREDIS != null)
            {
                CACHE_Access++;
                _redis_cache.Remove(user_id.ToString());
                String user = Encoding.UTF8.GetString(_InREDIS);
                BsonDocument db_user = BsonSerializer.Deserialize<BsonDocument>(user);
                db_user.Add("updated_at", timestamp);
                user = db_user.ToString();
                _redis_cache.Set(user_id.ToString(), Encoding.UTF8.GetBytes(user), new DistributedCacheEntryOptions());
            }

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var update = Builders<BsonDocument>.Update.Set("updated_at", timestamp);

            var db_update_result = _dbUser.UpdateOne(filter, update);
            DB_Access += (int)db_update_result.ModifiedCount;

            return db_update_result.ToBsonDocument();
        }

        public static BsonDocument UpdateUser_WithCache(int user_id)
        {
            //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var update = Builders<BsonDocument>.Update.Set("updated_at", GetTimestamp(DateTime.Now));

            var cache_update_result = _cacheUser.UpdateOne(filter, update);
            var db_update_result = _dbUser.UpdateOne(filter, update);

            CACHE_Access += (int)cache_update_result.ModifiedCount;
            DB_Access += (int)db_update_result.ModifiedCount;

            if (cache_update_result.ModifiedCount > 0)
            {
                //  Console.WriteLine("IN THE CACHE...\n");
                return cache_update_result.ToBsonDocument();  //.ToList() and .Count
            }
            else
            {
                //  Console.WriteLine("NOT IN THE CACHE...");
                return db_update_result.ToBsonDocument();
            }
        }

        public static BsonDocument UpdateUser_NoCache(int user_id)
        {
            //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var update = Builders<BsonDocument>.Update.Set("updated_at", GetTimestamp(DateTime.Now));

            var update_result = _dbUser.UpdateOne(filter, update);

            DB_Access += (int)update_result.ModifiedCount;
            return update_result.ToBsonDocument();
        }
        public static Boolean DeleteUser_WithCache(int user_id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            try
            {
                var _dbQuery = _dbUser.DeleteOne(filter);
                if (_dbQuery.IsAcknowledged && _dbQuery.DeletedCount == 1)
                {
                    DB_Access--;
                    var _cacheQuery = _cacheUser.DeleteOne(filter);
                    if (_cacheQuery.IsAcknowledged && _cacheQuery.DeletedCount == 1)
                    {
                        CACHE_Access--;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error");
            }
            return true;
        }

        public static Boolean DeleteUser_NoCache(int user_id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            try
            {
                var _dbQuery = _dbUser.DeleteOne(filter);
                if (_dbQuery.IsAcknowledged && _dbQuery.DeletedCount == 1)
                {
                    DB_Access--;
                    //Console.WriteLine("\nDeleted user {0} from db.", user_id);
                }
            }
            catch
            {
                Console.WriteLine("Error");
            }
            return true;
        }

        public static Boolean DeleteUser_RedisCache(int user_id)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            try
            {
                var _dbQuery = _dbUser.DeleteOne(filter);
                if (_dbQuery.IsAcknowledged && _dbQuery.DeletedCount == 1)
                {
                    //Console.WriteLine("\nDeleted user {0} from db.", user_id);
                    DB_Access--;
                    var _InREDIS = _redis_cache.Get(user_id.ToString());
                    if (_InREDIS != null)
                    {
                        CACHE_Access--;
                        _redis_cache.Remove(user_id.ToString());
                        //Console.WriteLine("\nDeleted user {0} from redis cache.", user_id);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error");
            }
            return true;
        }
        public static void Shuffle<T>(T[] ops, T[] idx)
        {
            Random rng = new Random();
            int n = ops.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = ops[n];
                ops[n] = ops[k];
                ops[k] = temp;
                T temp2 = idx[n];
                idx[n] = idx[k];
                idx[k] = temp2;
            }
        }
        public static Tuple<int[], int[]> CreateOperationsList(int N, float insertRatio, float updateRatio, float deleteRatio)
        {
            /*
            0 insert,            1 update,            2 delete,            3 find
             */
            int insertEnd = (int)(N * insertRatio);
            int updateEnd = insertEnd + (int)(N * updateRatio);
            int deleteEnd = updateEnd + (int)(N * deleteRatio);

            int[] ops = new int[N];
            int[] idx = new int[N];
            for (int i = 0; i < N; i++)
            {
                if (i < insertEnd) ops[i] = 0; else if (i < updateEnd) ops[i] = 1; else if (i < deleteEnd) ops[i] = 2; else ops[i] = 3;
                idx[i] = (i < insertEnd) ? i + USER_COUNT : i % (insertEnd + USER_COUNT);
            }
            Shuffle(ops, idx);
            return Tuple.Create(ops, idx);
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }
        public static BsonDocument UpdateUser(int user_id)
        {
            switch (MODE)
            {
                case MONGO_CACHE:
                    return UpdateUser_WithCache(user_id);
                case REDIS_CACHE:
                    return UpdateUser_RedisCache(user_id);
                case NO_CACHE:
                    return UpdateUser_NoCache(user_id);
                default:
                    return null;
            }
        }
        public static BsonDocument GetUser(int user_id)
        {
            switch (MODE)
            {
                case MONGO_CACHE:
                    return GetUser_WithCache(user_id);
                case REDIS_CACHE:
                    return GetUser_RedisCache(user_id);
                case NO_CACHE:
                    return GetUser_NoCache(user_id);
                default:
                    return null;
            }
        }
        public static Boolean DeleteUser(int user_id)
        {
            switch (MODE)
            {
                case MONGO_CACHE:
                    return DeleteUser_WithCache(user_id);
                case REDIS_CACHE:
                    return DeleteUser_RedisCache(user_id);
                case NO_CACHE:
                    return DeleteUser_NoCache(user_id);
                default:
                    return false;
            }
        }
        /* 
        public class User : BsonDocument{
            public string user_name;
            public int user_id;
        }
        */

        /* 
            int user_id = 230;
            String user_name = "Sammy";

            var new_user_document = new BsonDocument
            {
                { "name", user_name },
                { "user_id", user_id }
            };
            AddUser(new_user_document);

            GetUser_WithCache(200);
            GetUser_WithCache(230);
            GetUser_WithCache(250);
        */

        /* 
                    var new_user = new user();
                    new_user.name = "bibi";
                    new_user.user_id = 250;

                    _cacheUser.InsertOne(new_user);
        */

        /* 
                    var cursor = collection.Find(new BsonDocument()).ToCursor();
                    foreach (var document in cursor.ToEnumerable())
                    {
                        Console.WriteLine(document);   
                    }
        */

        /*
            DB_Access = 0;   CACHE_Access = 0;
            sw.Start();
            foreach(int id in getId_List){
                 GetUserAsync_WithCache(id);
            }
            sw.Stop();
            Console.WriteLine("\nMongoDB In-Memory CACHE : AsyncGET and SyncSET ::: TIME => {0}",sw.Elapsed);
            Console.WriteLine("IN DB: " + DB_Access + "  IN CACHE: " + CACHE_Access);
        */
    }
}