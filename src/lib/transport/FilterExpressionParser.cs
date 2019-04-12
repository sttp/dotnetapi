//******************************************************************************************************
//  FilterExpressionParser.cs - Gbtc
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
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace sttp.transport
{
    // TODO: Change this to work with new ANTLR grammar
    public class FilterExpressionParser
    {
        private static readonly Regex s_filterExpression = new Regex("(FILTER[ ]+(TOP[ ]+(?<MaxRows>\\d+)[ ]+)?(?<TableName>\\w+)[ ]+WHERE[ ]+(?<Expression>.+)[ ]+ORDER[ ]+BY[ ]+(?<SortField>\\w+))|(FILTER[ ]+(TOP[ ]+(?<MaxRows>\\d+)[ ]+)?(?<TableName>\\w+)[ ]+WHERE[ ]+(?<Expression>.+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex s_timetagExpression = new Regex("\\*(?<Offset>[+-]?\\d*\\.?\\d*)(?<Unit>\\w+)", RegexOptions.Compiled);

        /// <summary>
        /// Parses a standard FILTER styles expression into its constituent parts.
        /// </summary>
        /// <param name="filterExpression">Filter expression to parse.</param>
        /// <param name="tableName">Name of table in filter expression.</param>
        /// <param name="whereExpression">Where expression in filter expression.</param>
        /// <param name="sortField">Sort field, if any, in filter expression.</param>
        /// <param name="takeCount">Total row restriction, if any, in filter expression.</param>
        /// <returns><c>true</c> if filter expression was successfully parsed; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method can be safely called from multiple threads.
        /// </remarks>
        public static bool ParseFilterExpression(string filterExpression, out string tableName, out string whereExpression, out string sortField, out int takeCount)
        {
            tableName = null;
            whereExpression = null;
            sortField = null;
            takeCount = 0;

            if (string.IsNullOrWhiteSpace(filterExpression))
                return false;

            Match filterMatch;

            lock (s_filterExpression)
            {
                filterMatch = s_filterExpression.Match(filterExpression.ReplaceControlCharacters());
            }

            if (!filterMatch.Success)
                return false;

            tableName = filterMatch.Result("${TableName}").Trim();
            whereExpression = filterMatch.Result("${Expression}").Trim();
            sortField = filterMatch.Result("${SortField}").Trim();
            string maxRows = filterMatch.Result("${MaxRows}").Trim();

            if (string.IsNullOrEmpty(maxRows) || !int.TryParse(maxRows, out takeCount))
                takeCount = int.MaxValue;

            return true;
        }

        /// <summary>
        /// Parses a string formatted as an absolute or relative time tag.
        /// </summary>
        /// <param name="timetag">String formatted as an absolute or relative time tag.</param>
        /// <returns>
        /// <see cref="DateTime"/> representing the parsed <paramref name="timetag"/> string formatted as an absolute or relative time tag.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Relative times are parsed based on an offset to current time (UTC) specified by "*".
        /// </para>
        /// <para>
        /// The <paramref name="timetag"/> parameter can be specified in one of the following formats:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Time Format</term>
        ///         <description>Format Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term>12-30-2000 23:59:59.033</term>
        ///         <description>Absolute date and time.</description>
        ///     </item>
        ///     <item>
        ///         <term>*</term>
        ///         <description>Evaluates to <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-20s</term>
        ///         <description>Evaluates to 20 seconds before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-10m</term>
        ///         <description>Evaluates to 10 minutes before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-1h</term>
        ///         <description>Evaluates to 1 hour before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-1d</term>
        ///         <description>Evaluates to 1 day before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*+2d</term>
        ///         <description>Evaluates to 2 days from <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="timetag"/> parameter cannot be null or empty.</exception>
        /// <exception cref="FormatException"><paramref name="timetag"/> does not contain a valid string representation of a date and time.</exception>
        public static DateTime ParseTimeTag(string timetag)
        {
            if (string.IsNullOrWhiteSpace(timetag))
                throw new ArgumentNullException(nameof(timetag), "Timetag string cannot be null or empty.");

            DateTime dateTime;

            if (timetag.Contains("*"))
            {
                // Relative time is specified.
                // Examples:
                // 1) * (Now)
                // 2) *-20s (20 seconds ago)
                // 3) *-10m (10 minutes ago)
                // 4) *-1h (1 hour ago)
                // 5) *-1d (1 day ago)
                // 6) *+2d (2 days from now)

                dateTime = DateTime.UtcNow;
                timetag = timetag.RemoveWhiteSpace();

                if (timetag.Length > 1)
                {
                    Match timetagMatch;

                    lock (s_timetagExpression)
                    {
                        timetagMatch = s_timetagExpression.Match(timetag);
                    }

                    if (timetagMatch.Success)
                    {
                        double offset = double.Parse(timetagMatch.Result("${Offset}").Trim());
                        string unit = timetagMatch.Result("${Unit}").Trim().ToLower();

                        switch (unit[0])
                        {
                            case 's':
                                dateTime = dateTime.AddSeconds(offset);
                                break;
                            case 'm':
                                dateTime = dateTime.AddMinutes(offset);
                                break;
                            case 'h':
                                dateTime = dateTime.AddHours(offset);
                                break;
                            case 'd':
                                dateTime = dateTime.AddDays(offset);
                                break;
                        }
                    }
                    else
                    {
                        // Expression match failed, attempt to parse absolute time specification.
                        dateTime = DateTime.Parse(timetag, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
                    }
                }
            }
            else
            {
                // Absolute time is specified.
                dateTime = DateTime.Parse(timetag, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            }

            return dateTime;
        }

        // Input keys can use DataSource for filtering desired set of input or output measurements
        // based on any table and fields in the data set by using a filter expression instead of
        // a list of measurement ID's. The format is as follows:

        //  FILTER <TableName> WHERE <Expression> [ORDER BY <SortField>]

        // Source tables are expected to have at least the following fields:
        //
        //      ID          NVARCHAR    Measurement key formatted as: ArchiveSource:PointID
        //      SignalID    GUID        Unique identification for measurement
        //      PointTag    NVARCHAR    Point tag of measurement
        //      Adder       FLOAT       Adder to apply to value, if any (default to 0.0)
        //      Multiplier  FLOAT       Multiplier to apply to value, if any (default to 1.0)
        //
        // Could have used standard SQL syntax here but didn't want to give user the impression
        // that this a standard SQL expression when it isn't - so chose the word FILTER to make
        // consumer was aware that this was not SQL, but SQL "like". The WHERE clause expression
        // uses standard SQL syntax (it is simply the DataTable.Select filter expression).

        /// <summary>
        /// Parses input measurement keys from connection string setting.
        /// </summary>
        /// <param name="dataSource">The <see cref="DataSet"/> used to define input measurement keys.</param>
        /// <param name="value">Value of setting used to define input measurement keys, typically "inputMeasurementKeys".</param>
        /// <param name="measurementTable">Measurement table name used to load additional meta-data; this is not used when specifying a FILTER expression.</param>
        /// <returns>User selected input measurement keys.</returns>
        public static MeasurementKey[] ParseInputMeasurementKeys(DataSet dataSource, string value, string measurementTable = "ActiveMeasurements")
        {
            List<MeasurementKey> keys = new List<MeasurementKey>();
            MeasurementKey key;
            bool dataSourceAvailable = (object)dataSource != null;

            if (string.IsNullOrWhiteSpace(value))
                return keys.ToArray();

            value = value.Trim();

            if (dataSourceAvailable && ParseFilterExpression(value, out string tableName, out string expression, out string sortField, out int takeCount))
            {
                foreach (DataRow row in dataSource.Tables[tableName].Select(expression, sortField).Take(takeCount))
                {
                    key = MeasurementKey.LookUpOrCreate(row["SignalID"].ToNonNullString(Guid.Empty.ToString()).ConvertToType<Guid>(), row["ID"].ToString());

                    if (key != MeasurementKey.Undefined)
                        keys.Add(key);
                }
            }
            else
            {
                // Add manually defined measurement keys
                foreach (string item in value.Split(';'))
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;

                    if (Guid.TryParse(item, out Guid id))
                    {
                        // The item was parsed as a signal ID so do a straight lookup
                        key = MeasurementKey.LookUpBySignalID(id);

                        if (key == MeasurementKey.Undefined && dataSourceAvailable && dataSource.Tables.Contains(measurementTable))
                        {
                            DataRow[] filteredRows = dataSource.Tables[measurementTable].Select($"SignalID = '{id}'");

                            if (filteredRows.Length > 0)
                                MeasurementKey.TryCreateOrUpdate(id, filteredRows[0]["ID"].ToString(), out key);
                        }
                    }
                    else if (!MeasurementKey.TryParse(item, out key))
                    {
                        if (dataSourceAvailable && dataSource.Tables.Contains(measurementTable))
                        {
                            DataRow[] filteredRows;

                            // The item could not be parsed as a signal ID, but we do have a data source we can use to find the signal ID
                            filteredRows = dataSource.Tables[measurementTable].Select($"ID = '{item.Trim()}'");

                            if (filteredRows.Length == 0)
                                filteredRows = dataSource.Tables[measurementTable].Select($"PointTag = '{item.Trim()}'");

                            if (filteredRows.Length > 0)
                                key = MeasurementKey.LookUpOrCreate(filteredRows[0]["SignalID"].ToNonNullString(Guid.Empty.ToString()).ConvertToType<Guid>(), filteredRows[0]["ID"].ToString());
                        }

                        // If all else fails, attempt to parse the item as a measurement key
                        if (key == MeasurementKey.Undefined)
                        {
                            if (id == Guid.Empty)
                                throw new InvalidOperationException($"Could not parse input measurement definition \"{item}\" as a filter expression, measurement key, point tag or Guid");

                            throw new InvalidOperationException($"Measurement (targeted for input) with an ID of \"{item}\" is not defined or is not enabled");
                        }
                    }

                    if (key != MeasurementKey.Undefined)
                        keys.Add(key);
                }
            }

            return keys.ToArray();
        }
    }
}
