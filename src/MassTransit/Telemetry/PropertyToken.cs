﻿// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;


    public class PropertyToken :
        MessageTemplateToken
    {
        readonly int? _position;
        readonly string _rawText;

        /// <summary>
        /// Construct a <see cref="PropertyToken"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="rawText">The token as it appears in the message template.</param>
        /// <param name="format">The format applied to the property, if any.</param>
        /// <param name="alignment">The alignment applied to the property, if any.</param>
        /// <param name="destructuring">The destructuring strategy applied to the property, if any.</param>
        /// <param name="startIndex">The token's start index in the template.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PropertyToken(string propertyName, string rawText, string format = null, Alignment? alignment = null,
            Destructuring destructuring = Destructuring.Default, int startIndex = -1)
            : base(startIndex)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (rawText == null)
                throw new ArgumentNullException(nameof(rawText));
            PropertyName = propertyName;
            Format = format;
            Destructuring = destructuring;
            _rawText = rawText;
            Alignment = alignment;

            int position;
            if (int.TryParse(PropertyName, NumberStyles.None, CultureInfo.InvariantCulture, out position) &&
                position >= 0)
            {
                _position = position;
            }
        }

        /// <summary>
        /// The token's length.
        /// </summary>
        public override int Length => _rawText.Length;

        /// <summary>
        /// The property name.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Destructuring strategy applied to the property.
        /// </summary>
        public Destructuring Destructuring { get; }

        /// <summary>
        /// Format applied to the property.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// Alignment applied to the property.
        /// </summary>
        public Alignment? Alignment { get; }

        /// <summary>
        /// True if the property name is a positional index; otherwise, false.
        /// </summary>
        public bool IsPositional => _position.HasValue;

        /// <summary>
        /// Render the token to the output.
        /// </summary>
        /// <param name="properties">Properties that may be represented by the token.</param>
        /// <param name="output">Output for the rendered string.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        public override void Render(IReadOnlyDictionary<string,TelemetryLogEventPropertyValue> properties, TextWriter output, IFormatProvider formatProvider = null)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            TelemetryLogEventPropertyValue propertyValue;
            if (!properties.TryGetValue(PropertyName, out propertyValue))
            {
                output.Write(_rawText);
                return;
            }

            if (!Alignment.HasValue)
            {
                propertyValue.Render(output, Format, formatProvider);
                return;
            }

            var valueOutput = new StringWriter();
            propertyValue.Render(valueOutput, Format, formatProvider);
            var value = valueOutput.ToString();

            if (value.Length >= Alignment.Value.Width)
            {
                output.Write(value);
                return;
            }

            var pad = Alignment.Value.Width - value.Length;

            if (Alignment.Value.Direction == AlignmentDirection.Right)
                output.Write(new string(' ', pad));

            output.Write(value);

            if (Alignment.Value.Direction == AlignmentDirection.Left)
                output.Write(new string(' ', pad));
        }

        /// <summary>
        /// Try to get the integer value represented by the property name.
        /// </summary>
        /// <param name="position">The integer value, if present.</param>
        /// <returns>True if the property is positional, otherwise false.</returns>
        public bool TryGetPositionalValue(out int position)
        {
            if (_position == null)
            {
                position = 0;
                return false;
            }

            position = _position.Value;
            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            var pt = obj as PropertyToken;
            return pt != null &&
                pt.Destructuring == Destructuring &&
                pt.Format == Format &&
                pt.PropertyName == PropertyName &&
                pt._rawText == _rawText;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode() => PropertyName.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => _rawText;
    }
}