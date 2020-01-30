﻿using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonCryption.Converters
{
    internal sealed class EncryptedArrayConverter<T> : JsonConverter<T[]>
    {
        private readonly IDataProtector _dataProtector;
        private readonly JsonSerializerOptions _options;
        private readonly Type _elementType;

        public EncryptedArrayConverter(IDataProtector dataProtector, JsonSerializerOptions options)
        {
            _dataProtector = dataProtector;
            _options = options;
            _elementType = typeof(T);
        }

        public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            reader.Read();
            
            var elementConverter = GetElementConverter(options);

            var items = new List<T>();

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException();

                var cipherText = reader.GetString();
                reader.Read();

                var base64 = _dataProtector.Unprotect(cipherText);
                var bytes = Convert.FromBase64String(base64);
                var item = elementConverter.FromBytes(bytes);
                items.Add(item);
            }

            return items.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                var elementConverter = GetElementConverter(options);
                
                writer.WriteStartArray();

                foreach (var item in value)
                {
                    var bytes = elementConverter.ToBytes(item);
                    var base64 = Convert.ToBase64String(bytes);
                    var cipherText = _dataProtector.Protect(base64);
                    writer.WriteStringValue(cipherText);
                }

                writer.WriteEndArray();
            }
        }

        private EncryptedConverter<T> GetElementConverter(JsonSerializerOptions options)
        {
            if (!(Coordinator.Singleton.GetConverter(_elementType, options ?? _options) is EncryptedConverter<T> itemConverter))
                throw new JsonException();
            return itemConverter;
        }
    }
}
