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



        public static int InCACHE;
        public static int InDB;

        static void Main(string[] args)
        {
            Test();

            _dbClient = new MongoClient("mongodb://localhost:27020");
            _db = _dbClient.GetDatabase("userdb");
            _dbUser = _db.GetCollection<BsonDocument>("db_users");

            _cacheClient = new MongoClient("mongodb://localhost:27018");
            _cache = _cacheClient.GetDatabase("userdb");
            _cacheUser = _cache.GetCollection<BsonDocument>("db_users");

            _redis_cache = new RedisCache(new RedisCacheOptions
            { Configuration = "localhost", InstanceName = "db_users" }
            );


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

            Random rnd = new Random();
            int getIndexes = rnd.Next(1, 100);
            int setIndexes = rnd.Next(1, 1000);

            List<int> getId_List = new List<int>();

            for (int times = 0; times < 10000; times++)
            {
                int random = rnd.Next(1, 100);
                //  Console.Write(random + " ");
                getId_List.Add(random);
            }

            Stopwatch sw = new Stopwatch();


            sw.Start();
            for (int id = 1; id <= 1000; id++)
            {
                var new_doc = new BsonDocument
                {
                    { "name", "bibi"+id },
                    { "user_id", id }
                };
                AddUser(new_doc);
            }
            sw.Stop();
            Console.WriteLine("ADD USER TIME: {0}", sw.Elapsed);


            InDB = 0; InCACHE = 0;
            sw.Start();
            foreach (int id in getId_List)
            {
                //GetUser_RedisCache(id);
                //GetUser_NoCache(id);
                //GetUser_WithCache(id);
                //GetUserAsync_WithCache(id);
                DeleteUser_RedisCache(id);
            }
            sw.Stop();
            Console.WriteLine("\nTIME => {0}", sw.Elapsed);
            Console.WriteLine("IN DB: " + InDB + "  IN CACHE: " + InCACHE);


            /*
            InDB = 0;   InCACHE = 0;
            sw.Start();
            foreach(int id in getId_List){
                 GetUserAsync_WithCache(id);
            }
            sw.Stop();
            Console.WriteLine("\nMongoDB In-Memory CACHE : AsyncGET and SyncSET ::: TIME => {0}",sw.Elapsed);
            Console.WriteLine("IN DB: " + InDB + "  IN CACHE: " + InCACHE);
            */
        }

        public static BsonDocument GetUser_RedisCache(int user_id)
        {
            var _InREDIS = _redis_cache.Get(user_id.ToString());
            if (_InREDIS != null)
            {
                InCACHE++;
                return Encoding.UTF8.GetString(_InREDIS).ToBsonDocument();
            }
            else
            {
                var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                var db_query = _dbUser.Find(filter);
                if ((int)db_query.Count() > 0)
                {
                    InDB++;
                    String db_user_str = db_query.First().ToString();
                    _redis_cache.Set(user_id.ToString(), Encoding.UTF8.GetBytes(db_user_str), new DistributedCacheEntryOptions());
                    return db_user_str.ToBsonDocument();
                }
                else
                {
                    return new BsonDocument();
                }
            }
        }

        public static BsonDocument GetUser_WithCache(int user_id)
        {

            //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var cache_query = _cacheUser.Find(filter);

            if ((int)cache_query.Count() > 0)
            {
                InCACHE++;
                //  Console.WriteLine("IN THE CACHE...\n");
                return cache_query.First();  //.ToList() and .Count
            }
            else
            {
                InDB++;
                //  Console.WriteLine("NOT IN THE CACHE...");
                var db_query = _dbUser.Find(filter);
                if ((int)db_query.Count() > 0)
                {
                    //  Console.WriteLine("IN THE DATABASE...");
                    _cacheUser.InsertOne(db_query.First());
                    //  Console.WriteLine("ADDED TO THE CACHE...\n");
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
                    //  Console.WriteLine("NOT IN THE DATABASE AS WELL...\n");
                    return new BsonDocument();
                }
            }
            // ??? return new BsonDocument();
        }

        public static BsonDocument GetUser_NoCache(int user_id)
        {

            //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var db_query = _dbUser.Find(filter);

            if ((int)db_query.Count() > 0)
            {
                InDB++;
                //  Console.WriteLine("IN THE DATABASE...\n");
                return db_query.First();
            }
            else
            {
                //  Console.WriteLine("NOT IN THE DATABASE...\n");
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
                InCACHE++;

                Console.WriteLine("IN CACHE: ASYNC CALL ENDS..." + user_id);
                return cache_query.First();
            }
            else
            {
                var db_query = await _dbUser.FindAsync(filter);
                if ((int)db_query.ToList().Count > 0)
                {
                    InDB++;
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

            _dbUser.InsertOne(new_user);

            return true;
        }

        public static BsonDocument UpdateUser_RedisCache(int user_id){
            //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
            String timestamp = GetTimestamp(DateTime.Now);
            var _InREDIS = _redis_cache.Get(user_id.ToString());
            if (_InREDIS != null) {
                InCACHE++;
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
            
            if(_InREDIS == null)
            {
                InDB += (int)db_update_result.ModifiedCount;
            } 
            return db_update_result.ToBsonDocument();
        }

        public static BsonDocument UpdateUser_WithCache(int user_id){
            //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var update = Builders<BsonDocument>.Update.Set("updated_at", GetTimestamp(DateTime.Now));
            
            var cache_update_result = _cacheUser.UpdateOne(filter, update);
            var db_update_result = _dbUser.UpdateOne(filter, update);
            
            if(cache_update_result.ModifiedCount > 0)
            {
                InCACHE += (int)cache_update_result.ModifiedCount;
                //  Console.WriteLine("IN THE CACHE...\n");
                return cache_update_result.ToBsonDocument();  //.ToList() and .Count
            } 
            else
            {
                InDB += (int)db_update_result.ModifiedCount;
                //  Console.WriteLine("NOT IN THE CACHE...");
                return db_update_result.ToBsonDocument();
            }
        }

        public static BsonDocument UpdateUser_NoCache(int user_id){
            //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var update = Builders<BsonDocument>.Update.Set("updated_at", GetTimestamp(DateTime.Now));
            
            var update_result = _dbUser.UpdateOne(filter, update);
            
            InDB += (int)update_result.ModifiedCount;
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
                    //Console.WriteLine("\nDeleted user {0} from db.", user_id);
                    InDB--;
                    var _cacheQuery = _cacheUser.DeleteOne(filter);
                    if (_cacheQuery.IsAcknowledged && _cacheQuery.DeletedCount == 1)
                    {
                        InCACHE--;
                        //Console.WriteLine("\nDeleted user {0} from cache.", user_id);
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
                    InDB--;
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
                    InDB--;
                    var _cacheQuery = _cacheUser.DeleteOne(filter);
                    var _InREDIS = _redis_cache.Get(user_id.ToString());
                    if (_InREDIS != null)
                    {
                        InCACHE--;
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
                idx[i] = (i < insertEnd) ? i : i % insertEnd;
            }
            Shuffle(ops, idx);
            return Tuple.Create(ops, idx);
        }
        public static void Test()
        {
            int N=100000;
            Tuple<int[],int[]> test=CreateOperationsList(N,0.5f,0.2f,0.1f);
            int[] opArray=test.Item1;
            int[] idxArray=test.Item2;
            int[] count=new int[4];
            for (int i = 0; i < N; i++)
            {
                count[opArray[i]]++;
                Console.WriteLine("Operation: {0} : UserId: {1}",opArray[i],idxArray[i]);
            }
            Console.WriteLine("Insert: {0}",count[0]);
            Console.WriteLine("Update: {0}",count[1]);
            Console.WriteLine("Delete: {0}",count[2]);
            Console.WriteLine("Find: {0}",count[3]);
        }

        public static String GetTimestamp(DateTime value) {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        /* 
        public class User : BsonDocument{
            public string user_name;
            public int user_id;
        }
        */
    }
}