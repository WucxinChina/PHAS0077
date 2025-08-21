using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    // Creates standarized JSON message used in this system
    public static string CreateContent(SystemAction systemAction, string contentDetails)
    {
        Content content = new Content()
        {
            action = systemAction.ToString(),
            contentDetails = contentDetails
        };

        return JsonUtility.ToJson(content);
    }

    [Serializable]
    public struct NullableInt
    {
        public int Value;
        public bool HasValue;

        public NullableInt(int value)
        {
            Value = value;
            HasValue = true;
        }

        public static implicit operator NullableInt(int value) => new NullableInt(value);

        public static implicit operator NullableInt(NullableNull value) => new NullableInt();

        public static implicit operator int(NullableInt value) => value.Value;

        public static implicit operator int?(NullableInt value) => value.HasValue ? value.Value : new int?();
    }

    [Serializable]
    public struct NullableVector3
    {
        public Vector3 Value;
        public bool HasValue;

        public NullableVector3(Vector3 value)
        {
            Value = value;
            HasValue = true;
        }

        public static implicit operator NullableVector3(Vector3 value) => new NullableVector3(value);

        public static implicit operator NullableVector3(NullableNull value) => new NullableVector3();

        public static implicit operator Vector3(NullableVector3 value) => value.Value;

        public static implicit operator Vector3?(NullableVector3 value) => value.HasValue ? value.Value : new Vector3?();
    }

    public sealed class NullableNull
    {
        private NullableNull()
        { }
    }
}
