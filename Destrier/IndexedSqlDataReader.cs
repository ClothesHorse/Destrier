﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Destrier
{
    public class IndexedSqlDataReader : IDataReader
    {
        public IndexedSqlDataReader() : base() { }
        public IndexedSqlDataReader(SqlDataReader hostReader)
        {
            _dr = hostReader;
        }

        private SqlDataReader _dr = null;

        private Dictionary<String, Int32> _columnMap = null;
        public Dictionary<String, Int32> ColumnMap
        {
            get
            {
                if (_columnMap == null)
                    _columnMap = _dr.GetColumnMap();

                return _columnMap;
            }
            private set { _columnMap = value; }
        }

        public Boolean HasColumn(String columnName)
        {
            return ColumnMap.ContainsKey(columnName.ToLower());
        }

        public Int32? GetColumnIndex(String columnName)
        {
            var columnNameLower = columnName.ToLower();
            if (ColumnMap.ContainsKey(columnNameLower))
                return ColumnMap[columnNameLower];
            else
                return null;
        }

        public Boolean HasRows
        {
            get
            {
                return _dr.HasRows;
            }
        }

        #region IDataReader
        public void Close()
        {
            _dr.Close();
        }

        public int Depth
        {
            get { return _dr.Depth; }
        }

        public DataTable GetSchemaTable()
        {
            return _dr.GetSchemaTable();
        }

        public bool IsClosed
        {
            get { return _dr.IsClosed; }
        }

        public bool NextResult()
        {
            var result = _dr.NextResult();
            ColumnMap = _dr.GetColumnMap();
            return result;
        }

        public bool Read()
        {
            return _dr.Read();
        }

        public int RecordsAffected
        {
            get { return _dr.RecordsAffected; }
        }

        public void Dispose()
        {
            _dr.Dispose();
        }

        public int FieldCount
        {
            get { return _dr.FieldCount; }
        }

        public bool GetBoolean(int i)
        {
            return _dr.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            return _dr.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return _dr.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return _dr.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _dr.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public IDataReader GetData(int i)
        {
            return _dr.GetData(i);
        }

        public string GetDataTypeName(int i)
        {
            return _dr.GetDataTypeName(i);
        }

        public DateTime GetDateTime(int i)
        {
            return _dr.GetDateTime(i);
        }

        public decimal GetDecimal(int i)
        {
            return _dr.GetDecimal(i);
        }

        public double GetDouble(int i)
        {
            return _dr.GetDouble(i);
        }

        public Type GetFieldType(int i)
        {
            return _dr.GetFieldType(i);
        }

        public float GetFloat(int i)
        {
            return _dr.GetFloat(i);
        }

        public Guid GetGuid(int i)
        {
            return _dr.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            return _dr.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            return _dr.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            return _dr.GetInt64(i);
        }

        public string GetName(int i)
        {
            return _dr.GetName(i);
        }

        public int GetOrdinal(string name)
        {
            return _dr.GetOrdinal(name);
        }

        public string GetString(int i)
        {
            return _dr.GetString(i);
        }

        public object GetValue(int i)
        {
            return _dr.GetValue(i);
        }

        public int GetValues(object[] values)
        {
            return _dr.GetValues(values);
        }

        public bool IsDBNull(int i)
        {
            return _dr.IsDBNull(i);
        }

        public object this[string name]
        {
            get 
            {
                return _dr[name];
            }
        }

        public object this[int i]
        {
            get
            {
                return _dr[i];
            }
        }

        public static implicit operator IndexedSqlDataReader(SqlDataReader dr)
        {
            return new IndexedSqlDataReader(dr);
        }
        #endregion

        public T Get<T>(String columnName)
        {
            return (T)this.Get(typeof(T), columnName);
        }

        public object Get(Type resultType, String columnName)
        {
            Boolean isNullableType = ReflectionCache.IsNullableType(resultType);
            Type effectiveType = isNullableType ? ReflectionCache.GetUnderlyingTypeForNullable(resultType) : resultType;

            var columnIndex = GetColumnIndex(columnName);
            if (columnIndex != null)
            {
                if (!this.IsDBNull(columnIndex.Value))
                {
                    object value = this[columnIndex.Value];

                    if (effectiveType.IsEnum)
                        return Enum.ToObject(effectiveType, value);
                    else
                        return Convert.ChangeType(value, effectiveType);
                }
            }

            return isNullableType ? null : ReflectionCache.GetDefault(effectiveType);
        }

        public dynamic ReadDynamic()
        {
            var value = new AgileObject();
            if (this.HasRows)
            {
                while (this.Read())
                {
                    for (int i = 0; i < this.FieldCount; i++)
                    {
                        var name = this.GetName(i);
                        ((IDictionary<string, object>)value)[name] = this.GetValue(i);
                    }
                }
            }
            return value;
        }

        public List<AgileObject> ReadDynamicList(Boolean advanceToNextResultAfter = false)
        {
            List<AgileObject> values = new List<AgileObject>();
            if (this.HasRows)
            {
                while (this.Read())
                {
                    dynamic value = new AgileObject();
                    for (int i = 0; i < this.FieldCount; i++)
                    {
                        var name = this.GetName(i);
                        ((IDictionary<string, object>)value)[name] = this.GetValue(i);
                    }
                    values.Add(value);
                }
            }
            return values;
        }

        public  T ReadScalar<T>(Boolean advanceToNextResultAfter = true) where T : struct
        {
            T value = default(T);

            if (this.HasRows)
            {
                while (this.Read())
                {
                    if (!this.IsDBNull(0))
                        value = (T)Convert.ChangeType(this.GetValue(0), typeof(T));
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return value;
        }

        public  T ReadObject<T>(Boolean returnNullOnEmpty = false, Boolean advanceToNextResultAfter = true) where T : class, IPopulate
        {
            T newObject = ReflectionCache.GetNewObject<T>();
            if (this.HasRows)
            {
                while (this.Read())
                {
                    newObject.Populate(this);
                }
            }
            else if (returnNullOnEmpty)
            {
                newObject = null;
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return newObject;
        }

        public  List<T> ReadScalarList<T>(Boolean advanceToNextResultAfter = true)
        {
            List<T> list = new List<T>();

            if (this.HasRows)
            {
                while (this.Read())
                {
                    list.Add((T)Convert.ChangeType(this.GetValue(0), typeof(T)));
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return list;
        }

        public  List<T> ReadList<T>(Boolean columnsCanBeMissing = false, Boolean advanceToNextResultAfter = true) where T : class, IPopulate
        {
            List<T> list = new List<T>();

            if (this.HasRows)
            {
                while (this.Read())
                {
                    T newObject = ReflectionCache.GetNewObject<T>();
                    newObject.Populate(this);
                    list.Add(newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return list;
        }

        public  Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<TValue, TKey> keySelector, Boolean advanceToNextResultAfter = true) where TValue : class, IPopulate
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();

            if (this.HasRows)
            {
                while (this.Read())
                {
                    TValue newObject = ReflectionCache.GetNewObject<TValue>();
                    newObject.Populate(this);
                    TKey keyValue = keySelector(newObject);

                    if (!dict.ContainsKey(keyValue))
                        dict.Add(keyValue, newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();

            return dict;
        }

        public void ReadIntoParentCollection(Type type, Action<IndexedSqlDataReader, IPopulate> doStuffToAddToParent, Boolean advanceToNextResultAfter = true, Boolean populateFullResults = false)
        {
            if (this.HasRows)
            {
                while (this.Read())
                {
                    IPopulate newObject = ReflectionCache.GetNewObject(type) as IPopulate;
                    if (newObject == null)
                        throw new Exception("type is not IPopulate");

                    if (populateFullResults && newObject is BaseModel)
                    {
                        ((BaseModel)newObject).PopulateFullResults(this);
                    }
                    else
                        newObject.Populate(this);

                    doStuffToAddToParent(this, newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }

        public void ReadIntoParentCollection<T>(Action<IndexedSqlDataReader, T> doStuffToAddToParent, Boolean advanceToNextResultAfter = true) where T : class, IPopulate
        {
            if (this.HasRows)
            {
                while (this.Read())
                {
                    T newObject = ReflectionCache.GetNewObject<T>();
                    newObject.Populate(this);
                    doStuffToAddToParent(this, newObject);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }

        public void ReadFullControl(Action<IndexedSqlDataReader> action, Boolean advanceToNextResultAfter = true)
        {
            if (this.HasRows)
            {
                while (this.Read())
                {
                    action(this);
                }
            }

            if (advanceToNextResultAfter)
                this.NextResult();
        }

    }
}
