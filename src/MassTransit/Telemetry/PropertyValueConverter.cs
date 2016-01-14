// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Internals.Extensions;


    class PropertyValueConverter :
        ILogEventPropertyFactory,
        ILogEventPropertyValueFactory
    {
        static readonly HashSet<Type> BuiltInScalarTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(char),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(Uri)
        };

        readonly IDestructuringPolicy[] _destructuringPolicies;
        readonly int _maximumDestructuringDepth;
        readonly IScalarConversionPolicy[] _scalarConversionPolicies;

        public PropertyValueConverter(int maximumDestructuringDepth, IEnumerable<Type> additionalScalarTypes,
            IEnumerable<IDestructuringPolicy> additionalDestructuringPolicies)
        {
            if (additionalScalarTypes == null)
                throw new ArgumentNullException(nameof(additionalScalarTypes));
            if (additionalDestructuringPolicies == null)
                throw new ArgumentNullException(nameof(additionalDestructuringPolicies));
            if (maximumDestructuringDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumDestructuringDepth));

            _maximumDestructuringDepth = maximumDestructuringDepth;

            _scalarConversionPolicies = new IScalarConversionPolicy[]
            {
                new SimpleScalarConversionPolicy(BuiltInScalarTypes.Concat(additionalScalarTypes)),
                new NullableScalarConversionPolicy(),
                new EnumScalarConversionPolicy(),
                new ByteArrayScalarConversionPolicy(),
                new ReflectionTypesScalarConversionPolicy()
            };

            _destructuringPolicies = additionalDestructuringPolicies
                .Concat(new[]
                {
                    new DelegateDestructuringPolicy()
                })
                .ToArray();
        }

        public TelemetryLogEventProperty CreateProperty(string name, object value, bool destructureObjects = false)
        {
            return new TelemetryLogEventProperty(name, CreateValue(value, destructureObjects));
        }

        public TelemetryLogEventPropertyValue CreateValue(object value, bool destructureObjects = false)
        {
            return CreatePropertyValue(value, destructureObjects, 1);
        }

        public TelemetryLogEventPropertyValue CreateValue(object value, Destructuring destructuring)
        {
            return CreatePropertyValue(value, destructuring, 1);
        }

        TelemetryLogEventPropertyValue CreatePropertyValue(object value, bool destructureObjects, int depth)
        {
            return CreatePropertyValue(
                value,
                destructureObjects
                    ? Destructuring.Destructure
                    : Destructuring.Default,
                depth);
        }

        TelemetryLogEventPropertyValue CreatePropertyValue(object value, Destructuring destructuring, int depth)
        {
            if (value == null)
                return new ScalarValue(null);

            if (destructuring == Destructuring.Stringify)
                return new ScalarValue(value.ToString());

            var valueType = value.GetType();
            var limiter = new DepthLimiter(depth, _maximumDestructuringDepth, this);

            foreach (var scalarConversionPolicy in _scalarConversionPolicies)
            {
                ScalarValue converted;
                if (scalarConversionPolicy.TryConvertToScalar(value, limiter, out converted))
                    return converted;
            }

            if (destructuring == Destructuring.Destructure)
            {
                foreach (var destructuringPolicy in _destructuringPolicies)
                {
                    TelemetryLogEventPropertyValue result;
                    if (destructuringPolicy.TryDestructure(value, limiter, out result))
                        return result;
                }
            }

            var enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                // Only dictionaries with 'scalar' keys are permitted, as
                // more complex keys may not serialize to unique values for
                // representation in sinks. This check strengthens the expectation
                // that resulting dictionary is representable in JSON as well
                // as richer formats (e.g. XML, .NET type-aware...).
                // Only actual dictionaries are supported, as arbitrary types
                // can implement multiple IDictionary interfaces and thus introduce
                // multiple different interpretations.
                if (IsValueTypeDictionary(valueType))
                {
                    return new DictionaryValue(enumerable.Cast<dynamic>()
                        .Select(kvp => new KeyValuePair<ScalarValue, TelemetryLogEventPropertyValue>(
                            (ScalarValue)limiter.CreatePropertyValue(kvp.Key, destructuring),
                            limiter.CreatePropertyValue(kvp.Value, destructuring)))
                        .Where(kvp => kvp.Key.Value != null));
                }

                return new SequenceValue(
                    enumerable.Cast<object>().Select(o => limiter.CreatePropertyValue(o, destructuring)));
            }

            if (destructuring == Destructuring.Destructure)
            {
                var typeTag = value.GetType().Name;
                if (typeTag.Length <= 0 || !char.IsLetter(typeTag[0]))
                    typeTag = null;

                return new StructureValue(GetProperties(value, limiter), typeTag);
            }

            return new ScalarValue(value.ToString());
        }

        bool IsValueTypeDictionary(Type valueType)
        {
            return
                valueType.IsConstructedGenericType &&
                    valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                    IsValidDictionaryKeyType(
                        valueType.GenericTypeArguments
                            [0]);
        }

        bool IsValidDictionaryKeyType(Type valueType)
        {
            return BuiltInScalarTypes.Contains(valueType) ||
                valueType.GetTypeInfo().IsEnum;
        }

        static IEnumerable<TelemetryLogEventProperty> GetProperties(object value, ILogEventPropertyValueFactory recursive)
        {
            foreach (var prop in value.GetType().GetAllProperties())
            {
                object propValue;
                try
                {
                    propValue = prop.GetValue(value);
                }
                catch (TargetParameterCountException)
                {
                    TelemetryContext.CurrentOrDefault.Warning("The property accessor {0} is a non-default indexer", prop);
                    continue;
                }
                catch (TargetInvocationException ex)
                {
                    TelemetryContext.CurrentOrDefault.Warning("The property accessor {0} threw exception {1}", prop, ex);
                    propValue = "The property accessor threw an exception: " + ex.InnerException.GetType().Name;
                }
                yield return new TelemetryLogEventProperty(prop.Name, recursive.CreateValue(propValue, true));
            }
        }


        class DepthLimiter :
            ILogEventPropertyValueFactory
        {
            readonly int _currentDepth;
            readonly int _maximumDestructuringDepth;
            readonly PropertyValueConverter _propertyValueConverter;

            public DepthLimiter(int currentDepth, int maximumDepth, PropertyValueConverter propertyValueConverter)
            {
                _maximumDestructuringDepth = maximumDepth;
                _currentDepth = currentDepth;
                _propertyValueConverter = propertyValueConverter;
            }

            public TelemetryLogEventPropertyValue CreateValue(object value, bool destructureObjects = false)
            {
                return DefaultIfMaximumDepth() ??
                    _propertyValueConverter.CreatePropertyValue(value, destructureObjects, _currentDepth + 1);
            }

            public TelemetryLogEventPropertyValue CreatePropertyValue(object value, Destructuring destructuring)
            {
                return DefaultIfMaximumDepth() ??
                    _propertyValueConverter.CreatePropertyValue(value, destructuring, _currentDepth + 1);
            }

            TelemetryLogEventPropertyValue DefaultIfMaximumDepth()
            {
                if (_currentDepth == _maximumDestructuringDepth)
                {
                    TelemetryContext.CurrentOrDefault.Warning("Maximum destructuring depth reached.");
                    return new ScalarValue(null);
                }

                return null;
            }
        }
    }
}