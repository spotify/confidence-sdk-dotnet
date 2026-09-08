#nullable enable

using System;
using System.Collections.Generic;

namespace UnityOpenFeature.Core
{
    /// <summary>
    /// Data attached to a tracking event, mirroring the OpenFeature .NET SDK's
    /// <c>TrackingEventDetails</c>: immutable, with an <see cref="Empty"/>
    /// instance, accessor methods and a fluent builder.
    ///
    /// Two deliberate divergences from the official type, both forced by this
    /// Unity SDK not vendoring OpenFeature's value model:
    ///   * attribute values are <see cref="object"/> rather than OpenFeature's
    ///     <c>Value</c>/<c>Structure</c> wrappers, which do not exist here;
    ///   * <see cref="AsDictionary"/> returns an <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    ///     rather than an <c>IImmutableDictionary</c>, to avoid taking a
    ///     System.Collections.Immutable dependency into the Unity package.
    /// </summary>
    public sealed class TrackingEventDetails
    {
        private readonly Dictionary<string, object> data;

        private TrackingEventDetails()
        {
            this.data = new Dictionary<string, object>();
            this.Value = null;
        }

        internal TrackingEventDetails(Dictionary<string, object> content, double? value)
        {
            // Copied so a builder reused after Build() cannot mutate what it produced.
            this.data = new Dictionary<string, object>(content);
            this.Value = value;
        }

        /// <summary>The predefined numeric value of the tracking event, if any.</summary>
        public double? Value { get; }

        /// <summary>An empty, shared instance carrying no value and no attributes.</summary>
        public static TrackingEventDetails Empty { get; } = new TrackingEventDetails();

        /// <summary>The number of attributes, excluding <see cref="Value"/>.</summary>
        public int Count => this.data.Count;

        /// <summary>Creates a builder for a new instance.</summary>
        public static TrackingEventDetailsBuilder Builder()
        {
            return new TrackingEventDetailsBuilder();
        }

        /// <summary>
        /// Gets the attribute stored under <paramref name="key"/>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The key is not present.</exception>
        public object GetValue(string key)
        {
            return this.data[key];
        }

        /// <summary>Returns whether an attribute is stored under <paramref name="key"/>.</summary>
        public bool ContainsKey(string key)
        {
            return this.data.ContainsKey(key);
        }

        /// <summary>Gets the attribute under <paramref name="key"/> without throwing.</summary>
        public bool TryGetValue(string key, out object? value)
        {
            return this.data.TryGetValue(key, out value);
        }

        /// <summary>Returns the attributes as a read-only dictionary.</summary>
        public IReadOnlyDictionary<string, object> AsDictionary()
        {
            return this.data;
        }

        /// <summary>Enumerates the attributes.</summary>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this.data.GetEnumerator();
        }
    }

    /// <summary>
    /// Fluent builder for <see cref="TrackingEventDetails"/>. Intended for use by
    /// a single thread; the instance it builds is immutable and safe to share.
    /// </summary>
    public sealed class TrackingEventDetailsBuilder
    {
        private readonly Dictionary<string, object> data = new Dictionary<string, object>();
        private double? eventValue;

        internal TrackingEventDetailsBuilder()
        {
        }

        /// <summary>Sets the predefined numeric value of the tracking event.</summary>
        public TrackingEventDetailsBuilder SetValue(double? value)
        {
            this.eventValue = value;
            return this;
        }

        /// <summary>Sets an attribute.</summary>
        public TrackingEventDetailsBuilder Set(string key, object value)
        {
            this.data[key] = value;
            return this;
        }

        /// <summary>Sets a string attribute.</summary>
        public TrackingEventDetailsBuilder Set(string key, string value)
        {
            this.data[key] = value;
            return this;
        }

        /// <summary>Sets an integer attribute.</summary>
        public TrackingEventDetailsBuilder Set(string key, int value)
        {
            this.data[key] = value;
            return this;
        }

        /// <summary>Sets a double attribute.</summary>
        public TrackingEventDetailsBuilder Set(string key, double value)
        {
            this.data[key] = value;
            return this;
        }

        /// <summary>Sets a long attribute.</summary>
        public TrackingEventDetailsBuilder Set(string key, long value)
        {
            this.data[key] = value;
            return this;
        }

        /// <summary>Sets a boolean attribute.</summary>
        public TrackingEventDetailsBuilder Set(string key, bool value)
        {
            this.data[key] = value;
            return this;
        }

        /// <summary>Sets a <see cref="DateTime"/> attribute.</summary>
        public TrackingEventDetailsBuilder Set(string key, DateTime value)
        {
            this.data[key] = value;
            return this;
        }

        /// <summary>
        /// Incorporates an existing instance, overwriting attributes that collide.
        /// Its <see cref="TrackingEventDetails.Value"/> is adopted when set.
        /// </summary>
        public TrackingEventDetailsBuilder Merge(TrackingEventDetails trackingDetails)
        {
            if (trackingDetails == null)
            {
                return this;
            }

            foreach (var kvp in trackingDetails.AsDictionary())
            {
                this.data[kvp.Key] = kvp.Value;
            }

            if (trackingDetails.Value != null)
            {
                this.eventValue = trackingDetails.Value;
            }

            return this;
        }

        /// <summary>Builds an immutable <see cref="TrackingEventDetails"/>.</summary>
        public TrackingEventDetails Build()
        {
            return new TrackingEventDetails(this.data, this.eventValue);
        }
    }
}
