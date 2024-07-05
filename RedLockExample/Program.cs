using System;
using StackExchange.Redis;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

namespace Redlock 
{
    public class Program
    {
        static void Main()
        {
            // Redis bağlantılarını oluşturun
            var redisConnectionString = "localhost:6379";
            var redis = ConnectionMultiplexer.Connect(redisConnectionString);
            var redisDatabase = redis.GetDatabase();

            // Redlock konfigürasyonu
            var multiplexers = new List<RedLockMultiplexer> { redis };
            var redlockFactory = RedLockFactory.Create(multiplexers);

            // Rezervasyon işlemleri
            var tableId = "table_5";
            var reservationTime = "2024-07-04 19:00";

            // Aynı masa ve zaman için iki farklı istemcinin rezervasyon yapma girişimini simüle edin
            var restaurantTask = Task.Run(() => MakeReservation(redisDatabase, redlockFactory, tableId, reservationTime, "Restaurant"));
            var onlineTask = Task.Run(() => MakeReservation(redisDatabase, redlockFactory, tableId, reservationTime, "Online"));

            Task.WhenAll(restaurantTask, onlineTask);

            // Programın çalışmasını bitirmek için bir tuşa basın
            Console.ReadKey();
        }

        static async Task MakeReservation(IDatabase redisDatabase, RedLockFactory redlockFactory, string tableId, string reservationTime, string clientType)
        {
            // Dağıtık kilidi edinmeye çalış
            var resource = $"table:{tableId}:{reservationTime}";
            var expiry = TimeSpan.FromSeconds(30);

            using (var redlock = await redlockFactory.CreateLockAsync(resource, expiry))
            {
                if (redlock.IsAcquired)
                {
                    // Mevcut rezervasyon durumunu kontrol et
                    var currentReservation = await redisDatabase.StringGetAsync(resource);

                    // Masa uygun mu kontrol et
                    if (currentReservation.IsNullOrEmpty)
                    {
                        // Beklemenin amacı rezervasyon oluşturulurken zaman geçirmek ve ikinci threadin veriye erişmeye çalışmasını yakalamak.
                        Thread.Sleep(3000); 
                        await redisDatabase.StringSetAsync(resource, "reserved");
                        Console.WriteLine($"{clientType} client reserved table {tableId} at {reservationTime}");
                    }
                    else
                    {
                        Console.WriteLine($"{clientType} client could not reserve table {tableId} at {reservationTime} (already reserved)");
                    }
                }
                else
                {
                    Console.WriteLine($"{clientType} client could not acquire lock for table reservation");
                }
            }
        }
    }
}