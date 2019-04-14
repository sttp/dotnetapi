//******************************************************************************************************
//  TransportTypes.cs - Gbtc
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
//  04/14/2019 - J. Ritchie Carroll
//       Imported source code from Grid Solutions Framework.
//
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;

namespace sttp.transport
{
    /// <summary>
    /// Represents an interface for an abstract measurement value measured by a device at an exact time.
    /// </summary>
    /// <remarks>
    /// This interface abstractly represents a measured value at an exact time interval.
    /// </remarks>
    public interface IMeasurement : IComparable
    {
        /// <summary>
        /// Gets or sets associated metadata values for the <see cref="IMeasurement"/> .
        /// </summary>
        MeasurementMetadata Metadata
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="Guid"/> based signal ID of this <see cref="IMeasurement"/>.
        /// </summary>
        /// <remarks>
        /// This is the fundamental identifier of the <see cref="IMeasurement"/>.
        /// </remarks>
        Guid ID
        {
            get;
        }

        /// <summary>
        /// Gets or sets the primary key of this <see cref="IMeasurement"/>.
        /// </summary>
        MeasurementKey Key
        {
            get;
        }

        /// <summary>
        /// Gets or sets exact timestamp, in ticks, of the data represented by this <see cref="IMeasurement"/>.
        /// </summary>
        /// <remarks>
        /// The value of this property represents the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001.
        /// </remarks>
        Ticks Timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the raw typed value of this <see cref="IMeasurement"/>.
        /// </summary>
        double Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the adjusted numeric value of this <see cref="IMeasurement"/>, taking into account the specified <see cref="Adder"/> and <see cref="Multiplier"/> offsets.
        /// </summary>
        double AdjustedValue
        {
            get;
        }

        /// <summary>
        /// Defines an offset to add to the <see cref="IMeasurement"/> value.
        /// </summary>
        /// <remarks>
        /// Implementers should make sure this value defaults to zero.
        /// </remarks>
        double Adder
        {
            get;
        }

        /// <summary>
        /// Defines a multiplicative offset to apply to the <see cref="IMeasurement"/> value.
        /// </summary>
        /// <remarks>
        /// Implementers should make sure this value defaults to one.
        /// </remarks>
        double Multiplier
        {
            get;
        }

        /// <summary>
        /// Gets or sets <see cref="MeasurementStateFlags"/> associated with this <see cref="IMeasurement"/>.
        /// </summary>
        MeasurementStateFlags StateFlags
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a set of meta-data fields for <see cref="IMeasurement"/> that should rarely change.
    /// </summary>
    /// <remarks>
    /// This class allows measurement meta-data to be quickly transferred from one <see cref="IMeasurement"/> to 
    /// another. This class is immutable, so any change to these values requires that the class be recreated.
    /// </remarks>
    [Serializable]
    public class MeasurementMetadata
    {
        #region [ Members ]

        // Fields

        /// <summary>
        /// Gets or sets the primary key of this <see cref="IMeasurement"/>.
        /// </summary>
        public readonly MeasurementKey Key;

        /// <summary>
        /// Gets or sets the text based tag name of this <see cref="IMeasurement"/>.
        /// </summary>
        public readonly string TagName;

        /// <summary>
        /// Defines an offset to add to the <see cref="IMeasurement"/> value.
        /// </summary>
        /// <remarks>
        /// Implementers should make sure this value defaults to zero.
        /// </remarks>
        public readonly double Adder;

        /// <summary>
        /// Defines a multiplicative offset to apply to the <see cref="IMeasurement"/> value.
        /// </summary>
        /// <remarks>
        /// Implementers should make sure this value defaults to one.
        /// </remarks>
        public readonly double Multiplier;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a <see cref="MeasurementMetadata"/>
        /// </summary>
        /// <param name="key">Gets or sets the primary key of this <see cref="IMeasurement"/>.</param>
        /// <param name="tagName">Gets or sets the text based tag name of this <see cref="IMeasurement"/>.</param>
        /// <param name="adder">Defines an offset to add to the <see cref="IMeasurement"/> value.</param>
        /// <param name="multiplier">Defines a multiplicative offset to apply to the <see cref="IMeasurement"/> value.</param>
        public MeasurementMetadata(MeasurementKey key, string tagName, double adder, double multiplier)
        {
            Key = key;
            TagName = tagName;
            Adder = adder;
            Multiplier = multiplier;
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Creates a new instance of <see cref="MeasurementMetadata"/> using the provided measurement <paramref name="key"/>. All other fields remain the same.
        /// </summary>
        /// <param name="key">The key to set.</param>
        /// <returns>New instance of <see cref="MeasurementMetadata"/> using the provided measurement <paramref name="key"/>.</returns>
        public MeasurementMetadata ChangeKey(MeasurementKey key) => Key == key ? this : new MeasurementMetadata(key, TagName, Adder, Multiplier);

        /// <summary>
        /// Creates a new instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="adder"/>. All other fields remain the same.
        /// </summary>
        /// <param name="adder">The adder to set.</param>
        /// <returns>New instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="adder"/>.</returns>
        public MeasurementMetadata ChangeAdder(double adder) => Adder == adder ? this : new MeasurementMetadata(Key, TagName, adder, Multiplier);

        /// <summary>
        /// Creates a new instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="adder"/> and <paramref name="multiplier"/>. All other fields remain the same.
        /// </summary>
        /// <param name="adder">The adder to set.</param>
        /// <param name="multiplier">The multiplier to set.</param>
        /// <returns>New instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="adder"/> and <paramref name="multiplier"/>.</returns>
        public MeasurementMetadata ChangeAdderMultiplier(double adder, double multiplier) => Adder == adder && Multiplier == multiplier ? this : new MeasurementMetadata(Key, TagName, adder, multiplier);

        /// <summary>
        /// Creates a new instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="multiplier"/>. All other fields remain the same.
        /// </summary>
        /// <param name="multiplier">The multiplier to set.</param>
        /// <returns>New instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="multiplier"/>.</returns>
        public MeasurementMetadata ChangeMultiplier(double multiplier) => Multiplier == multiplier ? this : new MeasurementMetadata(Key, TagName, Adder, multiplier);

        /// <summary>
        /// Creates a new instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="tagName"/>. All other fields remain the same.
        /// </summary>
        /// <param name="tagName">The tag name to set.</param>
        /// <returns>New instance of <see cref="MeasurementMetadata"/> using the provided <paramref name="tagName"/>.</returns>
        public MeasurementMetadata ChangeTagName(string tagName) => TagName == tagName ? this : new MeasurementMetadata(Key, tagName, Adder, Multiplier);

        #endregion

        #region [ Static ]

        // Static Fields
        private static MeasurementMetadata s_undefined;

        // Static Properties

        /// <summary>
        /// Represents an undefined <see cref="MeasurementMetadata"/>.
        /// </summary>
        public static MeasurementMetadata Undefined => s_undefined;

        // Static Methods

        // Create measurement metadata for undefined
        internal static void CreateUndefinedMeasurementMetadata()
        {
            s_undefined = s_undefined ?? new MeasurementMetadata(MeasurementKey.Undefined, null, 0, 1);
        }

        #endregion
    }

    /// <summary>
    /// Represents a primary key for a measurement.
    /// </summary>
    [Serializable]
    public class MeasurementKey
    {
        #region [ Members ]

        // Fields
        private readonly Guid m_signalID;
        private ulong m_id;
        private string m_source;
        private readonly int m_hashCode;
        private readonly int m_runtimeID;
        private MeasurementMetadata m_metadata;

        #endregion

        #region [ Constructors ]

        private MeasurementKey(Guid signalID, ulong id, string source)
        {
            m_signalID = signalID;
            m_id = id;
            m_source = source;
            m_hashCode = base.GetHashCode();
            m_runtimeID = Interlocked.Increment(ref s_nextRuntimeID) - 1;
            m_metadata = new MeasurementMetadata(this, null, 0, 1);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets <see cref="Guid"/> ID of signal associated with this <see cref="MeasurementKey"/>.
        /// </summary>
        public Guid SignalID => m_signalID;

        /// <summary>
        /// Gets or sets the numeric ID of this <see cref="MeasurementKey"/>.
        /// </summary>
        public ulong ID => m_id;

        /// <summary>
        /// Gets or sets the source of this <see cref="MeasurementKey"/>.
        /// </summary>
        /// <remarks>
        /// This value is typically used to track the archive name in which the measurement, that this <see cref="MeasurementKey"/> represents, is stored.
        /// </remarks>
        public string Source => m_source;

        /// <summary>
        /// A unique ID that is assigned at runtime to identify this instance of <see cref="MeasurementKey"/>. 
        /// This value will change between life cycles, so it cannot be used to compare <see cref="MeasurementKey"/>
        /// instances that are running out of process or in a separate <see cref="AppDomain"/>.
        /// </summary>
        /// <remarks>
        /// Since each <see cref="SignalID"/> is only tied to a single <see cref="MeasurementKey"/> object, 
        /// this provides another unique identifier that is zero indexed. 
        /// This allows certain optimizations such as array lookups.
        /// </remarks>
        public int RuntimeID => m_runtimeID;

        /// <summary>
        /// Gets the <see cref="MeasurementMetadata"/> as loaded from metadata.
        /// </summary>
        /// <remarks>
        /// This is to be considered the reference value. Adapters are free to change this inside specific <see cref="IMeasurement"/> instances
        /// This value should only be updated upon change in the primary data source using <see cref="SetMeasurementMetadata"/>.
        /// </remarks>
        public MeasurementMetadata Metadata => m_metadata;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Updates the values of the <see cref="Metadata"/>.
        /// </summary>
        /// <param name="tagName">Gets or sets the text based tag name.</param>
        /// <param name="adder">Defines an offset to add to the <see cref="IMeasurement"/> value.</param>
        /// <param name="multiplier">Defines a multiplicative offset to apply to the <see cref="IMeasurement"/> value.</param>
        public void SetMeasurementMetadata(string tagName, double adder, double multiplier)
        {
            if (this == Undefined)
            {
                throw new NotSupportedException("Cannot set data source information for an undefined measurement.");
            }

            if (m_metadata.TagName != tagName || m_metadata.Adder != adder || m_metadata.Multiplier != multiplier)
            {
                m_metadata = new MeasurementMetadata(this, tagName, adder, multiplier);
            }
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="MeasurementKey"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="MeasurementKey"/>.</returns>
        public override string ToString()
        {
            return $"{m_source}:{m_id}";
        }

        /// <summary>
        /// Serves as a hash function for the current <see cref="MeasurementKey"/>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="MeasurementKey"/>.</returns>
        public override int GetHashCode()
        {
            return m_hashCode;
        }

        #endregion

        #region [ Static ]

        // Static Fields

        // All edits to <see cref="s_idCache"/> as well as the ConcurrentDictionaries in s_keyCache should occur within a lock on s_syncEdits
        private static readonly ConcurrentDictionary<Guid, MeasurementKey> s_idCache = new ConcurrentDictionary<Guid, MeasurementKey>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<ulong, MeasurementKey>> s_keyCache = new ConcurrentDictionary<string, ConcurrentDictionary<ulong, MeasurementKey>>(StringComparer.OrdinalIgnoreCase);
        private static readonly object s_syncEdits = new object();
        private static int s_nextRuntimeID;

        /// <summary>
        /// Represents an undefined measurement key.
        /// </summary>
        public static readonly MeasurementKey Undefined;

        // Static Constructor
        static MeasurementKey()
        {
            // Order of operations is critical here since MeasurementKey and MeasurementMetadata have a circular reference
            Undefined = CreateUndefinedMeasurementKey();
            MeasurementMetadata.CreateUndefinedMeasurementMetadata();
            Undefined.m_metadata = MeasurementMetadata.Undefined;
        }

        // Static Methods

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty</exception>
        /// <exception cref="FormatException">The value is not in the correct format for a <see cref="MeasurementKey"/> value.</exception>
        public static MeasurementKey CreateOrUpdate(Guid signalID, string value)
        {
            if (!TrySplit(value, out string source, out ulong id))
            {
                throw new FormatException("The value is not in the correct format for a MeasurementKey value");
            }

            return CreateOrUpdate(signalID, source, id);
        }

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="source">Source of the measurement that this <see cref="MeasurementKey"/> represents (e.g., name of archive).</param>
        /// <param name="id">Numeric ID of the measurement that this <see cref="MeasurementKey"/> represents.</param>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> cannot be null.</exception>
        public static MeasurementKey CreateOrUpdate(Guid signalID, string source, ulong id)
        {
            Func<Guid, MeasurementKey> addValueFactory;
            Func<Guid, MeasurementKey, MeasurementKey> updateValueFactory;

            if (signalID == Guid.Empty)
            {
                throw new ArgumentException("Unable to update undefined measurement key", nameof(signalID));
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentNullException(nameof(source), "MeasurementKey source cannot be null or empty");
            }

            addValueFactory = guid =>
            {
                // Create a new measurement key and add it to the KeyCache
                ConcurrentDictionary<ulong, MeasurementKey> idLookup = s_keyCache.GetOrAdd(source, s => new ConcurrentDictionary<ulong, MeasurementKey>());
                return idLookup[id] = new MeasurementKey(guid, id, source);
            };

            updateValueFactory = (guid, key) =>
            {
                ConcurrentDictionary<ulong, MeasurementKey> idLookup;

                // If existing measurement key is exactly the same as the
                // one we are trying to create, simply return that key
                if (key.ID == id && key.Source == source)
                {
                    return key;
                }

                // Update source and ID and re-insert it into the KeyCache
                key.m_source = source;
                key.m_id = id;

                idLookup = s_keyCache.GetOrAdd(source, s => new ConcurrentDictionary<ulong, MeasurementKey>());
                idLookup[id] = key;

                return key;
            };

            // https://msdn.microsoft.com/en-us/library/ee378675(v=vs.110).aspx
            //
            //     If you call AddOrUpdate simultaneously on different threads,
            //     addValueFactory may be called multiple times, but its key/value
            //     pair might not be added to the dictionary for every call.
            //
            // This lock prevents race conditions that might occur in the addValueFactory that
            // could cause different MeasurementKey objects to be written to the KeyCache and IDCache
            lock (s_syncEdits)
            {
                return s_idCache.AddOrUpdate(signalID, addValueFactory, updateValueFactory);
            }
        }

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <param name="key">The measurement key that was created or updated or <see cref="Undefined"/>.</param>
        /// <returns>True if the measurement key was successfully created or updated, false otherwise.</returns>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty.</exception>
        /// <exception cref="ArgumentNullException">Measurement key Source cannot be null.</exception>
        public static bool TryCreateOrUpdate(Guid signalID, string value, out MeasurementKey key)
        {
            try
            {
                key = CreateOrUpdate(signalID, value);
                return true;
            }
            catch
            {
                key = Undefined;
                return false;
            }
        }

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="source">Source of the measurement that this <see cref="MeasurementKey"/> represents (e.g., name of archive).</param>
        /// <param name="id">Numeric ID of the measurement that this <see cref="MeasurementKey"/> represents.</param>
        /// <param name="key">The measurement key that was created or updated or <see cref="Undefined"/>.</param>
        /// <returns>True if the measurement key was successfully created or updated, false otherwise.</returns>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> cannot be null.</exception>
        public static bool TryCreateOrUpdate(Guid signalID, string source, ulong id, out MeasurementKey key)
        {
            try
            {
                key = CreateOrUpdate(signalID, source, id);
                return true;
            }
            catch
            {
                key = Undefined;
                return false;
            }
        }

        /// <summary>
        /// Looks up the measurement key associated with the given signal ID.
        /// </summary>
        /// <param name="signalID">The signal ID of the measurement key.</param>
        /// <returns>The measurement key associated with the given signal ID.</returns>
        /// <remarks>
        /// If no measurement key is found with the given signal ID,
        /// this method returns <see cref="Undefined"/>.
        /// </remarks>
        public static MeasurementKey LookUpBySignalID(Guid signalID)
        {
            MeasurementKey key;

            // ReSharper disable once InconsistentlySynchronizedField
            if (signalID == Guid.Empty)
            {
                key = Undefined;
            }
            else if (!s_idCache.TryGetValue(signalID, out key))
            {
                key = Undefined;
            }

            return key;
        }

        /// <summary>
        /// Looks up the measurement key associated with the given source and ID.
        /// </summary>
        /// <param name="source">The source of the signal.</param>
        /// <param name="id">The source-specific unique integer identifier.</param>
        /// <returns>The measurement key associated with the given source and ID.</returns>
        /// <remarks>
        /// If no measurement key is found with the given source and ID,
        /// this method returns <see cref="Undefined"/>.
        /// </remarks>
        public static MeasurementKey LookUpBySource(string source, ulong id)
        {
            if (!s_keyCache.TryGetValue(source, out ConcurrentDictionary<ulong, MeasurementKey> idLookup))
            {
                return Undefined;
            }

            if (!idLookup.TryGetValue(id, out MeasurementKey key))
            {
                return Undefined;
            }

            return key;
        }

        /// <summary>
        /// Performs a lookup by signal ID and, failing that, attempts to create
        /// the key using the given signal ID and the parsed source, and ID.
        /// </summary>
        /// <param name="signalID">The signal ID of the key to be looked up.</param>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(Guid signalID, string value)
        {
            if (!TrySplit(value, out string source, out ulong id))
            {
                return LookUpOrCreate(signalID, Undefined.Source, Undefined.ID);
            }

            return LookUpOrCreate(signalID, source, id);
        }

        /// <summary>
        /// Performs a lookup by signal ID and, failing that, attempts to
        /// create the key using the given signal ID, source, and ID.
        /// </summary>
        /// <param name="signalID">The signal ID of the key to be looked up.</param>
        /// <param name="source">The source to use for the key if the lookup fails.</param>
        /// <param name="id">The ID to use for the key if the lookup fails.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(Guid signalID, string source, ulong id)
        {
            MeasurementKey key = LookUpBySignalID(signalID);

            if (key == Undefined && !TryCreateOrUpdate(signalID, source, id, out key))
            {
                return Undefined;
            }

            return key;
        }

        /// <summary>
        /// Performs a lookup by source and, failing that, attempts to create
        /// the key using a newly generated signal ID and the parsed source and ID.
        /// </summary>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(string value)
        {
            if (!TrySplit(value, out string source, out ulong id))
            {
                return Undefined;
            }

            return LookUpOrCreate(source, id);
        }

        /// <summary>
        /// Performs a lookup by source and, failing that, attempts to create
        /// the key using a newly generated signal ID and the given source and ID.
        /// </summary>
        /// <param name="source">The source to use for the key if the lookup fails.</param>
        /// <param name="id">The ID to use for the key if the lookup fails.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(string source, ulong id)
        {
            MeasurementKey key = LookUpBySource(source, id);

            if (key == Undefined && !TryCreateOrUpdate(Guid.NewGuid(), source, id, out key))
            {
                return Undefined;
            }

            return key;
        }

        /// <summary>
        /// Converts the string representation of a <see cref="MeasurementKey"/> into its value equivalent.
        /// </summary>
        /// <param name="value">A string representing the <see cref="MeasurementKey"/> to convert.</param>
        /// <returns>A <see cref="MeasurementKey"/> value equivalent the representation contained in <paramref name="value"/>.</returns>
        /// <exception cref="FormatException">The value is not in the correct format for a <see cref="MeasurementKey"/> value.</exception>
        public static MeasurementKey Parse(string value)
        {
            if (TryParse(value, out MeasurementKey key))
            {
                return key;
            }

            throw new FormatException("The value is not in the correct format for a MeasurementKey value");
        }

        /// <summary>
        /// Attempts to convert the string representation of a <see cref="MeasurementKey"/> into its value equivalent.
        /// </summary>
        /// <param name="value">A string representing the <see cref="MeasurementKey"/> to convert.</param>
        /// <param name="key">Output <see cref="MeasurementKey"/> in which to stored parsed value.</param>
        /// <returns>A <c>true</c> if <see cref="MeasurementKey"/>representation contained in <paramref name="value"/> could be parsed; otherwise <c>false</c>.</returns>
        public static bool TryParse(string value, out MeasurementKey key)
        {
            // Split the input into source and ID
            if (TrySplit(value, out string source, out ulong id))
            {
                // First attempt to look up an existing key
                key = LookUpBySource(source, id);

                if (key == Undefined)
                {
                    try
                    {
                        // Lookup failed - attempt to create it with a newly generated signal ID
                        key = CreateOrUpdate(Guid.NewGuid(), source, id);
                    }
                    catch
                    {
                        // source is null or empty
                        key = Undefined;
                    }
                }
            }
            else
            {
                // Incorrect format for a measurement key
                key = Undefined;
            }

            return key != Undefined;
        }

        /// <summary>
        /// Establish default <see cref="MeasurementKey"/> cache.
        /// </summary>
        /// <param name="metadata">The dataset table containing metadata.</param>
        /// <remarks>
        /// Source tables are expected to have at least the following fields:
        /// <code>
        ///      ID          NVARCHAR    Measurement key formatted as: ArchiveSource:PointID
        ///      SignalID    GUID        Unique identification for measurement
        /// </code>
        /// </remarks>
        public static void EstablishDefaultCache(DataTable metadata)
        {
            // Establish default measurement key cache
            foreach (DataRow measurement in metadata.Rows)
            {
                if (TrySplit(measurement["ID"].ToString(), out string source, out ulong id))
                {
                    CreateOrUpdate(measurement["SignalID"].ToNonNullString(Guid.Empty.ToString()).ConvertToType<Guid>(), source, id);
                }
            }
        }

        /// <summary>
        /// Creates the undefined measurement key. Used to initialize <see cref="Undefined"/>.
        /// </summary>
        private static MeasurementKey CreateUndefinedMeasurementKey()
        {
            MeasurementKey key = new MeasurementKey(Guid.Empty, ulong.MaxValue, "__");
            // Lock on s_syncEdits is not required since method is only called by the static constructor
            s_keyCache.GetOrAdd(key.Source, kcf => new ConcurrentDictionary<ulong, MeasurementKey>())[ulong.MaxValue] = key;
            return key;
        }

        /// <summary>
        /// Attempts to split the given string representation
        /// of a measurement key into a source and ID pair.
        /// </summary>
        private static bool TrySplit(string value, out string source, out ulong id)
        {
            string[] elem;

            if (!string.IsNullOrEmpty(value))
            {
                elem = value.Split(':');

                if (elem.Length == 2 && ulong.TryParse(elem[1].Trim(), out id))
                {
                    source = elem[0].Trim();
                    return true;
                }
            }

            source = Undefined.Source;
            id = Undefined.ID;
            return false;
        }

        #endregion
    }
}
