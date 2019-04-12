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
using System.Data;
using System.Runtime.CompilerServices;

namespace sttp
{
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
    }
}
