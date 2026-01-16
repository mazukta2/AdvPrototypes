using System;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// Utility class to temporary store data in JSON format
    /// </summary>
    internal static class JsonData
    {
        // {"value":true}
        private const int _startIndex = 9; // "{\"value\"".Length
        private const string _jsonPattern = @"{{""value"":{0}}}";
        private static readonly Dictionary<Type, Json> _factory = new Dictionary<Type, Json>();

        public static string ToJson(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var jsonObj = GetJsonObject(obj);

            jsonObj.Value = obj;

            var json = JsonUtility.ToJson(jsonObj, false);

            var value = json[_startIndex..^1];

            return value;
        }

        private static Json GetJsonObject(object obj)
        {
            if (!_factory.TryGetValue(obj.GetType(), out var jsonObj))
            {
                var jsonType = typeof(Json<>).MakeGenericType(obj.GetType());
                jsonObj = (Json)Activator.CreateInstance(jsonType);
                _factory[obj.GetType()] = jsonObj;
            }

            return jsonObj;
        }

        public static T FromJson<T>(string json)
        {
            var fullJson = string.Format(_jsonPattern, json);
            var jsonObj = JsonUtility.FromJson<Json<T>>(fullJson);
            return jsonObj.value;
        }

        public static object FromJson(string json, Type type)
        {
            var fullJson = string.Format(_jsonPattern, json);
            var jsonType = typeof(Json<>).MakeGenericType(type);
            var jsonObj = (Json)JsonUtility.FromJson(fullJson, jsonType);
            return jsonObj.Value;
        }

        private abstract class Json 
        { 
            public abstract object Value { get; set; }
        }

        [Serializable]
        private sealed class Json<T> : Json
        {
            public T value;

            public override object Value { get => value; set => this.value = (T)value; }
        }
    }
}
