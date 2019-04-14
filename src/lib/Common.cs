﻿//******************************************************************************************************
//  Common.cs - Gbtc
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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace sttp
{
    // This is the location for handy miscellaneous functions that are difficult to categorize elsewhere.

    /// <summary>
    /// Defines common global functions.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Determines if the current system is a POSIX style environment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Since a .NET application compiled under Mono can run under both Windows and Unix style platforms,
        /// you can use this property to easily determine the current operating environment.
        /// </para>
        /// <para>
        /// This property will return <c>true</c> for both MacOSX and Unix environments. Use the Platform property
        /// of the <see cref="System.Environment.OSVersion"/> to determine more specific platform type, e.g., 
        /// MacOSX or Unix. Note that all flavors of Linux will show up as <see cref="PlatformID.Unix"/>.
        /// </para>
        /// </remarks>        
        public static readonly bool IsPosixEnvironment = Path.DirectorySeparatorChar == '/';   // This is how Mono source often checks this

        /// <summary>
        /// Determines if the code base is currently running under Mono.
        /// </summary>
        /// <remarks>
        /// This property can be used to make a run-time determination if Windows or Mono based .NET is being used. However, it is
        /// highly recommended to use the MONO compiler directive wherever possible instead of determining this at run-time.
        /// </remarks>
        public static bool IsMono = (object)Type.GetType("Mono.Runtime") != null;

        // The following "ToNonNullString" methods extend all class based objects. Note that these extension methods can be
        // called even if the base object is null, hence the value of these methods. Our philosophy for this project has been
        // "not" to apply extensions to all objects (this to avoid general namespace pollution) and make sure extensions are
        // grouped together in their own source file (e.g., StringExtensions.cs); however these items do apply to all items
        // and are essentially type-less hence their location in the "Common" class. These extension methods are at least
        // limited to classes and won't show up on native types and custom structures.

        /// <summary>
        /// Converts value to string; null objects (or DBNull objects) will return an empty string (""). 
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of <see cref="Object"/> to convert to string.</typeparam>
        /// <param name="value">Value to convert to string.</param>
        /// <returns><paramref name="value"/> as a string; if <paramref name="value"/> is null, empty string ("") will be returned. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToNonNullString<T>(this T value) where T : class
        {
            return (object)value == null || value is DBNull ? "" : value.ToString();
        }

        /// <summary>
        /// Converts value to string; null objects (or DBNull objects) will return specified <paramref name="nonNullValue"/>.
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of <see cref="Object"/> to convert to string.</typeparam>
        /// <param name="value">Value to convert to string.</param>
        /// <param name="nonNullValue"><see cref="String"/> to return if <paramref name="value"/> is null.</param>
        /// <returns><paramref name="value"/> as a string; if <paramref name="value"/> is null, <paramref name="nonNullValue"/> will be returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nonNullValue"/> cannot be null.</exception>
        public static string ToNonNullString<T>(this T value, string nonNullValue) where T : class
        {
            if ((object)nonNullValue == null)
                throw new ArgumentNullException(nameof(nonNullValue));

            return (object)value == null || value is DBNull ? nonNullValue : value.ToString();
        }

        // We handle strings as a special version of the ToNullNullString extension to handle documentation a little differently

        /// <summary>
        /// Makes sure returned string value is not null; if this string is null, empty string ("") will be returned. 
        /// </summary>
        /// <param name="value"><see cref="String"/> to verify is not null.</param>
        /// <returns><see cref="String"/> value; if <paramref name="value"/> is null, empty string ("") will be returned.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToNonNullString(this string value)
        {
            return (object)value == null ? "" : value;
        }

        /// <summary>
        /// Converts value to string; null objects, DBNull objects or empty strings will return specified <paramref name="nonNullNorEmptyValue"/>.
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of <see cref="Object"/> to convert to string.</typeparam>
        /// <param name="value">Value to convert to string.</param>
        /// <param name="nonNullNorEmptyValue"><see cref="String"/> to return if <paramref name="value"/> is null.</param>
        /// <returns><paramref name="value"/> as a string; if <paramref name="value"/> is null, DBNull or an empty string <paramref name="nonNullNorEmptyValue"/> will be returned.</returns>
        /// <exception cref="ArgumentException"><paramref name="nonNullNorEmptyValue"/> must not be null or an empty string.</exception>
        public static string ToNonNullNorEmptyString<T>(this T value, string nonNullNorEmptyValue = " ") where T : class
        {
            if (string.IsNullOrEmpty(nonNullNorEmptyValue))
                throw new ArgumentException("Must not be null or an empty string", nameof(nonNullNorEmptyValue));

            if ((object)value == null || value is DBNull)
                return nonNullNorEmptyValue;

            string valueAsString = value.ToString();

            return string.IsNullOrEmpty(valueAsString) ? nonNullNorEmptyValue : valueAsString;
        }

        /// <summary>
        /// Converts value to string; null objects, DBNull objects, empty strings or all white space strings will return specified <paramref name="nonNullNorWhiteSpaceValue"/>.
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of <see cref="Object"/> to convert to string.</typeparam>
        /// <param name="value">Value to convert to string.</param>
        /// <param name="nonNullNorWhiteSpaceValue"><see cref="String"/> to return if <paramref name="value"/> is null.</param>
        /// <returns><paramref name="value"/> as a string; if <paramref name="value"/> is null, DBNull, empty or all white space, <paramref name="nonNullNorWhiteSpaceValue"/> will be returned.</returns>
        /// <exception cref="ArgumentException"><paramref name="nonNullNorWhiteSpaceValue"/> must not be null, an empty string or white space.</exception>
        public static string ToNonNullNorWhiteSpace<T>(this T value, string nonNullNorWhiteSpaceValue = "_") where T : class
        {
            if (string.IsNullOrWhiteSpace(nonNullNorWhiteSpaceValue))
                throw new ArgumentException("Must not be null, an empty string or white space", nameof(nonNullNorWhiteSpaceValue));

            if ((object)value == null || value is DBNull)
                return nonNullNorWhiteSpaceValue;

            string valueAsString = value.ToString();

            return string.IsNullOrWhiteSpace(valueAsString) ? nonNullNorWhiteSpaceValue : valueAsString;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to a <see cref="String"/> using an appropriate <see cref="TypeConverter"/>.
        /// </summary>
        /// <param name="value">Value to convert to a <see cref="String"/>.</param>
        /// <returns><paramref name="value"/> converted to a <see cref="String"/>.</returns>
        /// <remarks>
        /// <para>
        /// If <see cref="TypeConverter"/> fails, the value's <c>ToString()</c> value will be returned.
        /// Returned value will never be null, if no value exists an empty string ("") will be returned.
        /// </para>
        /// <para>
        /// You can use the <see cref="StringExtensions.ConvertToType{T}(string)"/> string extension
        /// method or <see cref="TypeConvertFromString(string, Type)"/> to convert the string back to its
        /// original <see cref="Type"/>.
        /// </para>
        /// </remarks>
        public static string TypeConvertToString(object value)
        {
            return TypeConvertToString(value, null);
        }

        /// <summary>
        /// Converts <paramref name="value"/> to a <see cref="String"/> using an appropriate <see cref="TypeConverter"/>.
        /// </summary>
        /// <param name="value">Value to convert to a <see cref="String"/>.</param>
        /// <param name="culture"><see cref="CultureInfo"/> to use for the conversion.</param>
        /// <returns><paramref name="value"/> converted to a <see cref="String"/>.</returns>
        /// <remarks>
        /// <para>
        /// If <see cref="TypeConverter"/> fails, the value's <c>ToString()</c> value will be returned.
        /// Returned value will never be null, if no value exists an empty string ("") will be returned.
        /// </para>
        /// <para>
        /// You can use the <see cref="StringExtensions.ConvertToType{T}(string, CultureInfo)"/> string
        /// extension method or <see cref="TypeConvertFromString(string, Type, CultureInfo)"/> to convert
        /// the string back to its original <see cref="Type"/>.
        /// </para>
        /// </remarks>
        public static string TypeConvertToString(object value, CultureInfo culture)
        {
            // Don't proceed further if value is null.
            if ((object)value == null)
                return string.Empty;

            // If value is already a string, no need to attempt conversion
            string valueAsString = value as string;

            if ((object)valueAsString != null)
                return valueAsString;

            // Initialize culture info if not specified.
            if ((object)culture == null)
                culture = CultureInfo.InvariantCulture;

            try
            {
                // Attempt to use type converter to set field value
                TypeConverter converter = TypeDescriptor.GetConverter(value);
                
                // ReSharper disable once AssignNullToNotNullAttribute
                return converter.ConvertToString(null, culture, value).ToNonNullString();
            }
            catch
            {
                // Otherwise just call object's ToString method
                return value.ToNonNullString();
            }
        }

        /// <summary>
        /// Converts this string into the specified type.
        /// </summary>
        /// <param name="value">Source string to convert to type.</param>
        /// <param name="type"><see cref="Type"/> to convert string to.</param>
        /// <returns>
        /// <see cref="string"/> converted to specified <see cref="Type"/>; default value of
        /// specified type if conversion fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This function makes use of a <see cref="TypeConverter"/> to convert <paramref name="value"/>
        /// to the specified <paramref name="type"/>, the best way to make sure <paramref name="value"/>
        /// can be converted back to its original type is to use the same <see cref="TypeConverter"/> to
        /// convert the original object to a <see cref="string"/>; see the
        /// <see cref="TypeConvertToString(object)"/> method for an easy way to do this.
        /// </para>
        /// <para>
        /// This function varies from <see cref="StringExtensions.ConvertToType{T}(string)"/>  in that it
        /// will use the default value for the <paramref name="type"/> parameter if <paramref name="value"/>
        /// is empty or <c>null</c>.
        /// </para>
        /// </remarks>
        public static object TypeConvertFromString(string value, Type type)
        {
            return TypeConvertFromString(value, type, null);
        }

        /// <summary>
        /// Converts this string into the specified type.
        /// </summary>
        /// <param name="value">Source string to convert to type.</param>
        /// <param name="type"><see cref="Type"/> to convert string to.</param>
        /// <param name="culture"><see cref="CultureInfo"/> to use for the conversion.</param>
        /// <returns>
        /// <see cref="string"/> converted to specified <see cref="Type"/>; default value of
        /// specified type if conversion fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This function makes use of a <see cref="TypeConverter"/> to convert <paramref name="value"/>
        /// to the specified <paramref name="type"/>, the best way to make sure <paramref name="value"/>
        /// can be converted back to its original type is to use the same <see cref="TypeConverter"/> to
        /// convert the original object to a <see cref="string"/>; see the
        /// <see cref="TypeConvertToString(object)"/> method for an easy way to do this.
        /// </para>
        /// <para>
        /// This function varies from <see cref="StringExtensions.ConvertToType{T}(string, CultureInfo)"/>
        /// in that it will use the default value for the <paramref name="type"/> parameter if
        /// <paramref name="value"/> is empty or <c>null</c>.
        /// </para>
        /// </remarks>
        public static object TypeConvertFromString(string value, Type type, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value))
                value = Activator.CreateInstance(type).ToString();

            return value.ConvertToType(type, culture);
        }

        /// <summary>
        /// Gets a high-resolution number of seconds, including fractional seconds, that have
        /// elapsed since 12:00:00 midnight, January 1, 0001.
        /// </summary>
        public static double SystemTimer => Ticks.ToSeconds(DateTime.UtcNow.Ticks);

        /// <summary>Determines if given item is equal to its default value (e.g., null or 0.0).</summary>
        /// <param name="item">Object to evaluate.</param>
        /// <returns>Result of evaluation as a <see cref="bool"/>.</returns>
        /// <remarks>
        /// Native types default to zero, not null, therefore this can be used to evaluate if an item is its default (i.e., uninitialized) value.
        /// </remarks>
        public static bool IsDefaultValue(object item)
        {
            // Only reference types can be null, therefore null is its default value
            if ((object)item == null)
                return true;

            Type itemType = item.GetType();

            if (!itemType.IsValueType)
                return false;

            // Handle common types
            IConvertible convertible = item as IConvertible;

            if ((object)convertible != null)
            {
                try
                {
                    switch (convertible.GetTypeCode())
                    {
                        case TypeCode.Boolean:
                            return (bool)item == default(bool);
                        case TypeCode.SByte:
                            return (sbyte)item == default(sbyte);
                        case TypeCode.Byte:
                            return (byte)item == default(byte);
                        case TypeCode.Int16:
                            return (short)item == default(short);
                        case TypeCode.UInt16:
                            return (ushort)item == default(ushort);
                        case TypeCode.Int32:
                            return (int)item == default(int);
                        case TypeCode.UInt32:
                            return (uint)item == default(uint);
                        case TypeCode.Int64:
                            return (long)item == default(long);
                        case TypeCode.UInt64:
                            return (ulong)item == default(ulong);
                        case TypeCode.Single:
                            return (float)item == default(float);
                        case TypeCode.Double:
                            return (double)item == default(double);
                        case TypeCode.Decimal:
                            return (decimal)item == default(decimal);
                        case TypeCode.Char:
                            return (char)item == default(char);
                        case TypeCode.DateTime:
                            return (DateTime)item == default(DateTime);
                    }
                }
                catch (InvalidCastException)
                {
                    // An exception here indicates that the item is a custom type that
                    // lied about its type code. The type should still be instantiable,
                    // so we can ignore this exception
                }
            }

            // Handle custom value types
            return ((ValueType)item).Equals(Activator.CreateInstance(itemType));
        }

        /// <summary>Determines if given item is a reference type.</summary>
        /// <param name="item">Object to evaluate.</param>
        /// <returns>Result of evaluation as a <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReference(object item)
        {
            return !(item is ValueType);
        }

        /// <summary>Determines if given item is a reference type but not a string.</summary>
        /// <param name="item">Object to evaluate.</param>
        /// <returns>Result of evaluation as a <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNonStringReference(object item)
        {
            return IsReference(item) && !(item is string);
        }

        /// <summary>
        /// Determines if given <paramref name="item"/> is a numeric type.
        /// </summary>
        /// <param name="item">Object to evaluate.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is a numeric type; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumericType(object item)
        {
            IConvertible convertible = item as IConvertible;

            if ((object)convertible != null)
            {
                switch (convertible.GetTypeCode())
                {
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if given <paramref name="item"/> is, or can be interpreted to be, a numeric type.
        /// </summary>
        /// <param name="item">Object to evaluate.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is, or can be interpreted to be, a numeric type; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If type of <paramref name="item"/> is a <see cref="char"/> or a <see cref="string"/>, then if value can be parsed as a numeric
        /// value, result will be <c>true</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(object item)
        {
            if (IsNumericType(item))
                return true;

            if (item is char || item is string)
                return double.TryParse(item.ToString(), out double _);

            return false;
        }

        /// <summary>Returns the smallest item from a list of parameters.</summary>
        /// <typeparam name="T">Return type <see cref="Type"/> that is the minimum value in the <paramref name="itemList"/>.</typeparam>
        /// <param name="itemList">A variable number of parameters of the specified type.</param>
        /// <returns>Result is the minimum value of type <see cref="Type"/> in the <paramref name="itemList"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T>(params T[] itemList)
        {
            return itemList.Min();
        }

        /// <summary>Returns the largest item from a list of parameters.</summary>
        /// <typeparam name="T">Return type <see cref="Type"/> that is the maximum value in the <paramref name="itemList"/>.</typeparam>
        /// <param name="itemList">A variable number of parameters of the specified type .</param>
        /// <returns>Result is the maximum value of type <see cref="Type"/> in the <paramref name="itemList"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T>(params T[] itemList)
        {
            return itemList.Max();
        }

        /// <summary>Returns the value that is neither the largest nor the smallest.</summary>
        /// <typeparam name="T"><see cref="Type"/> of the objects passed to and returned from this method.</typeparam>
        /// <param name="value1">Value 1.</param>
        /// <param name="value2">Value 2.</param>
        /// <param name="value3">Value 3.</param>
        /// <returns>Result is the value that is neither the largest nor the smallest.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Mid<T>(T value1, T value2, T value3) where T : IComparable<T>
        {
            if ((object)value1 == null)
                throw new ArgumentNullException(nameof(value1));

            if ((object)value2 == null)
                throw new ArgumentNullException(nameof(value2));

            if ((object)value3 == null)
                throw new ArgumentNullException(nameof(value3));

            int comp1to2 = value1.CompareTo(value2);
            int comp1to3 = value1.CompareTo(value3);
            int comp2to3 = value2.CompareTo(value3);

            // If 3 is the smallest, pick the smaller of 1 and 2
            if (comp1to3 >= 0 && comp2to3 >= 0)
                return comp1to2 <= 0 ? value1 : value2;

            // If 2 is the smallest, pick the smaller of 1 and 3
            if (comp1to2 >= 0 && comp2to3 <= 0)
                return comp1to3 <= 0 ? value1 : value3;

            // 1 is the smallest so pick the smaller of 2 and 3
            return comp2to3 <= 0 ? value2 : value3;
        }

        /// <summary>
        /// Returns <paramref name="value"/> if not <c>null</c>; otherwise <paramref name="nonNullValue"/>.
        /// </summary>
        /// <param name="value">Value to test.</param>
        /// <param name="nonNullValue">Value to return if primary value is null.</param>
        /// <returns><paramref name="value"/> if not <c>null</c>; otherwise <paramref name="nonNullValue"/>.</returns>
        /// <remarks>
        /// This function is useful when using evaluated code parsers based on older versions of .NET, e.g.,
        /// the RazorEngine or the ExpressionEvaluator.
        /// </remarks>
        public static object NotNull(object value, object nonNullValue)
        {
            if (nonNullValue == null)
                return new ArgumentNullException(nameof(nonNullValue));

            return value == null || value is DBNull ? nonNullValue : value;
        }
    }
}
