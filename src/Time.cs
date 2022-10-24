//******************************************************************************************************
//  Time.cs - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  09/20/2022 - Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using Gemstone;

namespace sttp;

/// <summary>
/// Defines constants and functions for tick values, 64-bit integers used to designate time in STTP.
/// </summary>
/// <remarks>
/// A tick value represents the number of 100-nanosecond intervals that have elapsed since 12:00:00
/// midnight, January 1, 0001 UTC, Gregorian calendar. A single tick represents one hundred nanoseconds,
/// or one ten-millionth of a second. There are 10,000 ticks in a millisecond and 10 million ticks in
/// a second. Only bits 01 to 62 (0x3FFFFFFFFFFFFFFF) are used to represent the timestamp value.
/// Bit 64 (0x8000000000000000) is used to denote leap second, i.e., second 60, where actual second
/// value would remain at 59. Bit 63 (0x4000000000000000) is used to denote leap second direction,
/// 0 for add, 1 for delete.
/// </remarks>
public static class Time
{
    /// <summary>
    /// Defines the flag (64th bit) that marks a Ticks value as a leap second, i.e., second 60 (one beyond normal second 59).
    /// </summary>
    public const ulong LeapSecondFlag = 1UL << 63;


    /// <summary>
    /// Defines the flag (63rd bit) that indicates if leap second is positive or negative; 0 for add, 1 for delete.
    /// </summary>
    public const ulong LeapSecondDirection = 1UL << 62;

    /// <summary>
    /// Defines mask for bits 1 to 62 that make up the value portion of a Ticks that represent time.
    /// </summary>
    public const ulong ValueMask = ~LeapSecondFlag & ~LeapSecondDirection;

    /// <summary>
    /// Defines the Ticks representation of the Unix epoch timestamp starting at January 1, 1970.
    /// </summary>
    public const ulong UnixBaseOffset = 621355968000000000;

    /// <summary>
    /// Gets <see cref="Ticks"/> timestamp for the specified STTP <paramref name="timestamp"/>.
    /// </summary>
    /// <param name="timestamp">STTP uint64 timestamp.</param>
    /// <returns><see cref="Ticks"/> representing specified STTP <paramref name="timestamp"/>.</returns>
    /// <remarks>
    /// <see cref="Ticks"/> value can be implicitly cast to and from standard .NET <see cref="DateTime"/>
    /// and <see cref="TimeSpan"/> instances.
    /// </remarks>
    public static Ticks ToTicks(ulong timestamp) => new((long)(timestamp & ValueMask));

    /// <summary>
    /// Gets STTP timestamp for the specified <paramref name="ticks"/>.
    /// </summary>
    /// <param name="ticks"><see cref="Ticks"/> timestamp.</param>
    /// <param name="leapSecond">Flag that determines if <paramref name="ticks"/> represents a leap second.</param>
    /// <param name="leapSecondIsNegative">Flag that determines if leap second is negative.</param>
    /// <returns>STTP uint64 timestamp representing specified <paramref name="ticks"/>.</returns>
    /// <remarks>
    /// <see cref="Ticks"/> value can be implicitly cast to and from standard .NET <see cref="DateTime"/>
    /// and <see cref="TimeSpan"/> instances.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="leapSecond"/> must be true if <paramref name="leapSecondIsNegative"/> is true.</exception>
    public static ulong FromTicks(Ticks ticks, bool leapSecond = false, bool leapSecondIsNegative = false)
    {
        ulong timestamp = (ulong)ticks.Value;

        if (!leapSecond && leapSecondIsNegative)
            throw new ArgumentException($"{nameof(leapSecond)} must be true if {nameof(leapSecondIsNegative)} is true");

        if (leapSecond)
            timestamp |= LeapSecondFlag;

        if (leapSecondIsNegative)
            timestamp |= LeapSecondDirection;

        return timestamp;
    }

    /// <summary>
    /// Gets flag that determines if <paramref name="timestamp"/> represents a leap second, i.e., second 60.
    /// </summary>
    /// <param name="timestamp">STTP uint64 timestamp.</param>
    /// <returns>Flag that determines if <paramref name="timestamp"/> represents a leap second.</returns>
    public static bool IsLeapSecond(ulong timestamp) => (timestamp & LeapSecondFlag) > 0;

    /// <summary>
    /// Gets flags that determines if <paramref name="timestamp"/> represents a negative leap second, i.e.,
    /// checks flag on second 58 to see if second 59 will be missing.
    /// </summary>
    /// <param name="timestamp">STTP uint64 timestamp.</param>
    /// <returns>Flag that determines if <paramref name="timestamp"/> represents a negative leap second.</returns>
    public static bool IsNegativeLeapSecond(ulong timestamp) => IsLeapSecond(timestamp) && (timestamp & LeapSecondDirection) > 0;
}    
