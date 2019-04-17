//******************************************************************************************************
//  CommonExtensions.cs - Gbtc
//
//  Copyright © 2019, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  04/12/2019 - J. Ritchie Carroll
//       Imported source code from Grid Solutions Framework.
//
//******************************************************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace sttp
{
    /// <summary>
    /// This class is used internally do define a standard buffer size.
    /// </summary>
    internal static class Standard
    {
        public const int BufferSize = 262144; // 256K
    }

    /// <summary>
    /// Defines extension functions related to <see cref="Array"/> manipulation.
    /// </summary>
    public static class CommonExtensions
    {
        /// <summary>
        /// Validates that the specified <paramref name="startIndex"/> and <paramref name="length"/> are valid within the given <paramref name="array"/>.
        /// </summary>
        /// <param name="array">Array to validate.</param>
        /// <param name="startIndex">0-based start index into the <paramref name="array"/>.</param>
        /// <param name="length">Valid number of items within <paramref name="array"/> from <paramref name="startIndex"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> or <paramref name="length"/> is less than 0 -or- 
        /// <paramref name="startIndex"/> and <paramref name="length"/> will exceed <paramref name="array"/> length.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateParameters<T>(this T[] array, int startIndex, int length)
        {
            if ((object)array == null || startIndex < 0 || length < 0 || startIndex + length > array.Length)
                RaiseValidationError(array, startIndex, length);
        }

        // This method will raise the actual error - this is needed since .NET will not inline anything that might throw an exception
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RaiseValidationError<T>(T[] array, int startIndex, int length)
        {
            if ((object)array == null)
                throw new ArgumentNullException(nameof(array));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "cannot be negative");

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "cannot be negative");

            if (startIndex + length > array.Length)
                throw new ArgumentOutOfRangeException(nameof(length), $"startIndex of {startIndex} and length of {length} will exceed array size of {array.Length}");
        }

        /// <summary>
        /// Returns a copy of the specified portion of the <paramref name="array"/> array.
        /// </summary>
        /// <param name="array">Source array.</param>
        /// <param name="startIndex">Offset into <paramref name="array"/> array.</param>
        /// <param name="length">Length of <paramref name="array"/> array to copy at <paramref name="startIndex"/> offset.</param>
        /// <returns>A array of data copied from the specified portion of the source array.</returns>
        /// <remarks>
        /// <para>
        /// Returned array will be extended as needed to make it the specified <paramref name="length"/>, but
        /// it will never be less than the source array length - <paramref name="startIndex"/>.
        /// </para>
        /// <para>
        /// If an existing array of primitives is already available, using the <see cref="Buffer.BlockCopy"/> directly
        /// instead of this extension method may be optimal since this method always allocates a new return array.
        /// Unlike <see cref="Buffer.BlockCopy"/>, however, this function also works with non-primitive types.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> is outside the range of valid indexes for the source array -or-
        /// <paramref name="length"/> is less than 0.
        /// </exception>
        public static T[] BlockCopy<T>(this T[] array, int startIndex, int length)
        {
            if ((object)array == null)
                throw new ArgumentNullException(nameof(array));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "cannot be negative");

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "cannot be negative");

            if (startIndex >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "not a valid index into the array");

            T[] copiedBytes = new T[array.Length - startIndex < length ? array.Length - startIndex : length];

            if (typeof(T).IsPrimitive)
                Buffer.BlockCopy(array, startIndex, copiedBytes, 0, copiedBytes.Length);
            else
                Array.Copy(array, startIndex, copiedBytes, 0, copiedBytes.Length);

            return copiedBytes;
        }

        /// <summary>Compares two arrays.</summary>
        /// <param name="array1">The first type array to compare to.</param>
        /// <param name="array2">The second type array to compare against.</param>
        /// <param name="orderIsImportant"><c>true</c> if order of elements should be considered for equality; otherwise, <c>false</c>.</param>
        /// <returns>An <see cref="int"/> which returns 0 if they are equal, 1 if <paramref name="array1"/> is larger, or -1 if <paramref name="array2"/> is larger.</returns>
        /// <typeparam name="TSource">The generic type of the array.</typeparam>
        /// <exception cref="ArgumentException">Cannot compare multidimensional arrays.</exception>
        public static int CompareTo<TSource>(this TSource[] array1, TSource[] array2, bool orderIsImportant = true)
        {
            return CompareTo(array1, array2, Comparer<TSource>.Default, orderIsImportant);
        }

        /// <summary>Compares two arrays.</summary>
        /// <param name="array1">The first <see cref="Array"/> to compare to.</param>
        /// <param name="array2">The second <see cref="Array"/> to compare against.</param>
        /// <param name="comparer">An interface <see cref="IComparer"/> that exposes a method to compare the two arrays.</param>
        /// <param name="orderIsImportant"><c>true</c> if order of elements should be considered for equality; otherwise, <c>false</c>.</param>
        /// <returns>An <see cref="int"/> which returns 0 if they are equal, 1 if <paramref name="array1"/> is larger, or -1 if <paramref name="array2"/> is larger.</returns>
        /// <remarks>This is a default comparer to make arrays comparable.</remarks>
        /// <exception cref="ArgumentException">Cannot compare multidimensional arrays.</exception>
        private static int CompareTo(this Array array1, Array array2, IComparer comparer, bool orderIsImportant = true)
        {
            if ((object)comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            if ((object)array1 == null && (object)array2 == null)
                return 0;

            if ((object)array1 == null)
                return -1;

            if ((object)array2 == null)
                return 1;

            if (array1.Rank != 1 || array2.Rank != 1)
                throw new ArgumentException("Cannot compare multidimensional arrays");

            // For arrays that do not have the same number of elements, the array with most elements
            // is assumed to be larger.
            if (array1.Length != array2.Length)
                return array1.Length.CompareTo(array2.Length);

            if (!orderIsImportant)
            {
                array1 = array1.Cast<object>().ToArray();
                array2 = array2.Cast<object>().ToArray();

                Array.Sort(array1, comparer);
                Array.Sort(array2, comparer);
            }

            int comparison = 0;

            for (int x = 0; x < array1.Length; x++)
            {
                comparison = comparer.Compare(array1.GetValue(x), array2.GetValue(x));

                if (comparison != 0)
                    break;
            }

            return comparison;
        }

        /// <summary>
        /// Provides strongly-typed access to each of the column values in the specified row.
        /// Automatically applies type conversion to the column values.
        /// </summary>
        /// <typeparam name="T">A generic parameter that specifies the return type of the column.</typeparam>
        /// <param name="row">The input <see cref="DataRow"/>, which acts as the this instance for the extension method.</param>
        /// <param name="field">The name of the column to return the value of.</param>
        /// <returns>The value, of type T, of the <see cref="DataColumn"/> specified by <paramref name="field"/>.</returns>
        public static T ConvertField<T>(this DataRow row, string field)
        {
            return ConvertField(row, field, default(T));
        }

        /// <summary>
        /// Provides strongly-typed access to each of the column values in the specified row.
        /// Automatically applies type conversion to the column values.
        /// </summary>
        /// <typeparam name="T">A generic parameter that specifies the return type of the column.</typeparam>
        /// <param name="row">The input <see cref="DataRow"/>, which acts as the this instance for the extension method.</param>
        /// <param name="field">The name of the column to return the value of.</param>
        /// <param name="defaultValue">The value to be substituted if <see cref="DBNull.Value"/> is retrieved.</param>
        /// <returns>The value, of type T, of the <see cref="DataColumn"/> specified by <paramref name="field"/>.</returns>
        public static T ConvertField<T>(this DataRow row, string field, T defaultValue)
        {
            object value = row.Field<object>(field);

            if (value == null || value == DBNull.Value)
                return defaultValue;

            // If the value is an instance of the given type,
            // no type conversion is necessary
            if (value is T variable)
                return variable;

            Type type = typeof(T);

            // Nullable types cannot be used in type conversion, but we can use Nullable.GetUnderlyingType()
            // to determine whether the type is nullable and convert to the underlying type instead
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // Handle Guids as a special case since they do not implement IConvertible
            if (underlyingType == typeof(Guid))
                return (T)(object)Guid.Parse(value.ToString());

            // Handle enums as a special case since they do not implement IConvertible
            if (underlyingType.IsEnum)
                return (T)Enum.Parse(underlyingType, value.ToString());

            return (T)Convert.ChangeType(value, underlyingType);
        }

        /// <summary>
        /// Automatically applies type conversion to column values when only a type is available.
        /// </summary>
        /// <param name="row">The input <see cref="DataRow"/>, which acts as the this instance for the extension method.</param>
        /// <param name="field">The name of the column to return the value of.</param>
        /// <param name="type">Type of the column.</param>
        /// <returns>The value of the <see cref="DataColumn"/> specified by <paramref name="field"/>.</returns>
        public static object ConvertField(this DataRow row, string field, Type type)
        {
            return ConvertField(row, field, type, null);
        }

        /// <summary>
        /// Automatically applies type conversion to column values when only a type is available.
        /// </summary>
        /// <param name="row">The input <see cref="DataRow"/>, which acts as the this instance for the extension method.</param>
        /// <param name="field">The name of the column to return the value of.</param>
        /// <param name="type">Type of the column.</param>
        /// <param name="defaultValue">The value to be substituted if <see cref="DBNull.Value"/> is retrieved.</param>
        /// <returns>The value of the <see cref="DataColumn"/> specified by <paramref name="field"/>.</returns>
        public static object ConvertField(this DataRow row, string field, Type type, object defaultValue)
        {
            object value = row.Field<object>(field);

            if (value == null || value == DBNull.Value)
                return defaultValue ?? (type.IsValueType ? Activator.CreateInstance(type) : null);

            // If the value is an instance of the given type,
            // no type conversion is necessary
            if (type.IsInstanceOfType(value))
                return value;

            // Nullable types cannot be used in type conversion, but we can use Nullable.GetUnderlyingType()
            // to determine whether the type is nullable and convert to the underlying type instead
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // Handle Guids as a special case since they do not implement IConvertible
            if (underlyingType == typeof(Guid))
                return Guid.Parse(value.ToString());

            // Handle enums as a special case since they do not implement IConvertible
            if (underlyingType.IsEnum)
                return Enum.Parse(underlyingType, value.ToString());

            return Convert.ChangeType(value, underlyingType);
        }

        /// <summary>
        /// Reads entire <see cref="Stream"/> contents, and returns <see cref="byte"/> array of data.
        /// </summary>
        /// <param name="source">The <see cref="Stream"/> to be converted to <see cref="byte"/> array.</param>
        /// <returns>An array of <see cref="byte"/>.</returns>
        public static byte[] ReadStream(this Stream source)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                source.CopyTo(outStream);
                return outStream.ToArray();
            }
        }

        /// <summary>
        /// Returns a binary array of encrypted data for the given parameters.
        /// </summary>
        /// <param name="algorithm"><see cref="SymmetricAlgorithm"/> to use for encryption.</param>
        /// <param name="data">Source buffer containing data to encrypt.</param>
        /// <param name="startIndex">Offset into <paramref name="data"/> buffer.</param>
        /// <param name="length">Number of bytes in <paramref name="data"/> buffer to encrypt starting from <paramref name="startIndex"/> offset.</param>
        /// <param name="key">The secret key to use for the symmetric algorithm.</param>
        /// <param name="iv">The initialization vector to use for the symmetric algorithm.</param>
        /// <returns>Encrypted version of <paramref name="data"/> buffer.</returns>
        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, byte[] data, int startIndex, int length, byte[] key, byte[] iv)
        {
            // Fastest to use existing buffer in non-expandable memory stream for source and large block allocated memory stream for destination
            using (MemoryStream source = new MemoryStream(data, startIndex, length))
            using (MemoryStream destination = new MemoryStream())
            {
                algorithm.Encrypt(source, destination, key, iv);
                return destination.ToArray();
            }
        }

        /// <summary>
        /// Encrypts input stream onto output stream for the given parameters.
        /// </summary>
        /// <param name="algorithm"><see cref="SymmetricAlgorithm"/> to use for encryption.</param>
        /// <param name="source">Source stream that contains data to encrypt.</param>
        /// <param name="destination">Destination stream used to hold encrypted data.</param>
        /// <param name="key">The secret key to use for the symmetric algorithm.</param>
        /// <param name="iv">The initialization vector to use for the symmetric algorithm.</param>
        public static void Encrypt(this SymmetricAlgorithm algorithm, Stream source, Stream destination, byte[] key, byte[] iv)
        {
            byte[] buffer = new byte[Standard.BufferSize];
            CryptoStream encodeStream = new CryptoStream(destination, algorithm.CreateEncryptor(key, iv), CryptoStreamMode.Write);

            // Encrypts data onto output stream.
            int read = source.Read(buffer, 0, Standard.BufferSize);

            while (read > 0)
            {
                encodeStream.Write(buffer, 0, read);
                read = source.Read(buffer, 0, Standard.BufferSize);
            }

            encodeStream.FlushFinalBlock();
        }

        /// <summary>
        /// Returns a binary array of decrypted data for the given parameters.
        /// </summary>
        /// <param name="algorithm"><see cref="SymmetricAlgorithm"/> to use for decryption.</param>
        /// <param name="data">Source buffer containing data to decrypt.</param>
        /// <param name="startIndex">Offset into <paramref name="data"/> buffer.</param>
        /// <param name="length">Number of bytes in <paramref name="data"/> buffer to decrypt starting from <paramref name="startIndex"/> offset.</param>
        /// <param name="key">The secret key to use for the symmetric algorithm.</param>
        /// <param name="iv">The initialization vector to use for the symmetric algorithm.</param>
        /// <returns>Decrypted version of <paramref name="data"/> buffer.</returns>
        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] data, int startIndex, int length, byte[] key, byte[] iv)
        {
            // Fastest to use existing buffer in non-expandable memory stream for source and large block allocated memory stream for destination
            using (MemoryStream source = new MemoryStream(data, startIndex, length))
            using (MemoryStream destination = new MemoryStream())
            {
                algorithm.Decrypt(source, destination, key, iv);
                return destination.ToArray();
            }
        }

        /// <summary>
        /// Decrypts input stream onto output stream for the given parameters.
        /// </summary>
        /// <param name="algorithm"><see cref="SymmetricAlgorithm"/> to use for decryption.</param>
        /// <param name="source">Source stream that contains data to decrypt.</param>
        /// <param name="destination">Destination stream used to hold decrypted data.</param>
        /// <param name="key">The secret key to use for the symmetric algorithm.</param>
        /// <param name="iv">The initialization vector to use for the symmetric algorithm.</param>
        public static void Decrypt(this SymmetricAlgorithm algorithm, Stream source, Stream destination, byte[] key, byte[] iv)
        {
            byte[] buffer = new byte[Standard.BufferSize];
            CryptoStream decodeStream = new CryptoStream(destination, algorithm.CreateDecryptor(key, iv), CryptoStreamMode.Write);

            // Decrypts data onto output stream.
            int read = source.Read(buffer, 0, Standard.BufferSize);

            while (read > 0)
            {
                decodeStream.Write(buffer, 0, read);
                read = source.Read(buffer, 0, Standard.BufferSize);
            }

            decodeStream.FlushFinalBlock();
        }
    }
}
