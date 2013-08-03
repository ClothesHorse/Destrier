﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Destrier.Redis.Core;

namespace Destrier.Redis.Core
{
    public class RedisClient : IDisposable
    {
        private RedisConnection _connection = null;

        public RedisClient(String host, int port = 6379, String password = null)
        {
            Host = host;
            Port = port;
            Password = password;
            _connection = RedisConnectionPool.GetConnection(host, port, password);
            _connection.Connect();
        }

        public RedisConnection Connection { get { return _connection; } }

        public String Host { get; set; }
        public Int32 Port { get; set; }
        public String Password { get; set; }

        public Boolean MultiSet(IDictionary<String, String> values)
        {
            var data = new List<String>();
            foreach (var kvp in values)
            {
                data.Add(kvp.Key);
                data.Add(kvp.Value);
            }
            _connection.Send("MSET", data.ToArray());
            return _connection.ReadData().IsSuccess;
        }

        public Boolean Set(String key, String value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot be null or empty.", "value");

            _connection.Send("SET", key, value);
            return _connection.ReadData().IsSuccess;
        }

        public Boolean SetIfNotExists(String key, String value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Cannot be null or empty.", "value");

            _connection.Send("SETNX", key, value);
            return _connection.ReadData().IsSuccess;
        }

        public IDictionary<String, String> MultiGet(params object[] args)
        {
            if (args == null || !args.Any())
                throw new ArgumentException("Cannot be null or empty.", "args");

            _connection.Send("MGET", args);
        }

        public String Get(String key)
        {
            return GetRawValue(key).StringValue;
        }

        public RedisDataValue GetRawValue(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send("GET", key);
            return _connection.ReadData();
        }

        public RedisKeyType KeyTypeOf(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send("TYPE", key);
            var keyType = _connection.ReadData().StringValue;
            switch (keyType)
            {
                case "none":
                    return RedisKeyType.None;
                case "string":
                    return RedisKeyType.String;
                case "set":
                    return RedisKeyType.Set;
                case "list":
                    return RedisKeyType.List;
                default:
                    throw new RedisException("Unknown Key Type Returned.");
            }
        }

        public Boolean ContainsKey(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send("EXISTS", key);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public Boolean Remove(String key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Cannot be null or empty.", "key");

            _connection.Send("DEL", key);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public Boolean Remove(IEnumerable<String> keys)
        {
            if (keys == null || !keys.Any())
                throw new ArgumentException("Cannot be null or empty.", "keys");

            _connection.Send("DEL", String.Join(",", keys));
            return _connection.ReadData().LongValue >= 1;
        }

        public String RandomKey()
        {
            _connection.Send("RANDOMKEY");
            return _connection.ReadData().StringValue;
        }

        public IEnumerable<String> GetKeys(String pattern = "*")
        {
            _connection.Send("KEYS", pattern);
            var result = _connection.ReadData().StringValue;

            if (String.IsNullOrEmpty(result))
                return Enumerable.Empty<String>();

            return result.Split(' ');
        }

        public Boolean RenameKey(String from, String to)
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException("Cannot be null or empty.", "from");

            if (string.IsNullOrEmpty(to))
                throw new ArgumentException("Cannot be null or empty.", "to");

            _connection.Send("RENAME", from, to);
            var result = _connection.ReadData().StringValue;
            return result.StartsWith("+");
        }

        public long Increment(String key, int? count = null)
        {
            if (count == null)
                _connection.Send("INCR", key);
            else
                _connection.Send("INCRBY", key, count.Value);

            return _connection.ReadData().LongValue;
        }

        public long Decrement(String key, int? count = null)
        {
            if (count == null)
                _connection.Send("DECR", key);
            else
                _connection.Send("DECRBY", key, count.Value);

            return _connection.ReadData().LongValue;
        }

        public bool Expire(String key, int seconds)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send("EXPIRE", key, seconds);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public bool ExpireAt(String key, DateTime time)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var unixTimestamp = RedisDataFormat.ToUnixTimestamp(time);
            _connection.Send("EXPIRE", key, unixTimestamp);
            return _connection.ReadData().LongValue.Equals(1);
        }

        public TimeSpan TimeToLive(String key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            _connection.Send("TTL", key);
            var result = _connection.ReadData().LongValue;
            return TimeSpan.FromSeconds((double)result);
        }

        public long GetDBSize()
        {
            _connection.Send("DBSIZE");
            return _connection.ReadData().LongValue;
        }

        public void Save()
        {
            _connection.Send("SAVE");
        }

        public void BackgroundSave()
        {
            _connection.Send("BGSAVE");
        }

        public void Shutdown()
        {
            _connection.Send("SHUTDOWN");
        }

        public void FlushAll()
        {
            _connection.Send("FLUSHALL");
        }

        public void FlushDb()
        {
            _connection.Send("FLUSHDB");
        }

        public DateTime GetLastSave()
        {
            _connection.Send("LASTSAVE");
            return _connection.ReadData().DateTimeValue;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.OnConnectionReleased();
                _connection = null;
            }
        }
    }
}
