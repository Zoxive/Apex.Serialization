using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Apex.Serialization.Tests
{
    public class NullableGenericValueTypeTests : IDisposable
    {
        private readonly IBinary _serializer;

        [Fact]
        public void NullableGenericValueType()
        {
            var x = new Option(Guid.Empty, null);
            RoundTrip(x);
            
            var y = new Option(Guid.Empty, ImmutableArray<string>.Empty);
            RoundTrip(y);
        }

        private T RoundTrip<T>(T p0)
        {
            using var memoryStream = new MemoryStream();
            _serializer.Write(p0, memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            
            var readValue = _serializer.Read<T>(memoryStream);
            return readValue;
        }

        public NullableGenericValueTypeTests()
        {
            _serializer = Binary.Create(new Settings{ SerializationMode = Mode.Graph }
                .MarkSerializable(typeof(ImmutableArray<>))
                .MarkSerializable(typeof(Option))
            );
        }

        public void Dispose()
        {
            _serializer.Dispose();
        }
    }
    
    public sealed class Option
    {
        public Guid Id { get; }
        public ImmutableArray<string>? SelectedValues { get; }

        public Option(Guid id, ImmutableArray<string>? selectedValues)
        {
            Id = id;
            SelectedValues = selectedValues;
        }
    }
}