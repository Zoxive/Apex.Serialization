﻿# Apex.Serialization

A high performance contract-less binary serializer capable of handling data trees or object graphs.

Suitable for realtime workloads where the serialized data will not persist for long, as most assembly changes will render the data format incompatible with older versions.

### Status

[![Build Status](https://numenfall.visualstudio.com/Libraries/_apis/build/status/dbolin.Apex.Serialization?branchName=master)](https://numenfall.visualstudio.com/Libraries/_build/latest?definitionId=11&branchName=master) <!-- [![Tests](https://img.shields.io/azure-devops/tests/numenfall/Libraries/11.svg?compact_message)](https://numenfall.visualstudio.com/Libraries/_build/latest?definitionId=11&branchName=master) -->
[![Code Coverage](https://img.shields.io/azure-devops/coverage/numenfall/Libraries/11/master.svg)](https://numenfall.visualstudio.com/Libraries/_build/latest?definitionId=11&branchName=master)

[Nuget Package](https://www.nuget.org/packages/Apex.Serialization/)

### Limitations

As the serialization is contract-less, the binary format produced depends on precise characteristics of the types serialized. Most changes to types, such as adding or removing fields, renaming types, or changing relationships between types will break compatibility with previously serialized data.  Serializing and deserializing between different chip architectures and .NET runtimes is not supported.

For performance reasons, the serializer and deserializer make use of pointers and direct memory access.  This will often cause attempting to deserialize incompatible data to immediately crash the application instead of throwing an exception.

NEVER deserialize data from an untrusted source.

Some types aren't supported:
- Objects that use randomized hashing or other runtime specific data to determine their behavior (including HashSet<>, Dictionary<,> and their immutable counterparts)
- Objects containing pointers or handles to unmanaged resources
- BlockingCollection\<>
- Non-generic standard collections

Requires code generation capabilities

### Usage

Serialization
```csharp
var obj = ClassToSerialize();
var binarySerializer = Binary.Create();
binarySerializer.Write(obj, outputStream);
```

Deserialization
```csharp
var obj = binarySerializer.Read<SerializedClassType>(inputStream)
```

Class instances are not thread safe, static methods are thread safe unless otherwise noted in their documentation. 

Always reuse serializer instances when possible, as the instance caches a lot of data to improve performance when repeatedly serializing or deserializing objects.  Since the instances are not thread-safe, you should use an object pool or some other method to ensure that only one thread uses an instance at a time.

Fields with the [Nonserialized] attribute will not be serialized or deserialized.

#### Settings

You may pass a Settings object to Binary.Create that lets you choose:
- between tree or graph serialization (graph serialization is required for cases where you have a cyclical reference or need to maintain object identity)
- whether functions should be serialized
- whether serialization hooks should be called (any methods with the [AfterDeserialization] attribute will be called after the object graph is completely deserialized.)

#### Performance

Performance is a feature!  Apex.Serialization is an extremely fast binary serializer.  See [benchmarks](Benchmarks.md) for comparisons with other fast binary serializers.

### Custom serialization/deserialization

You can define custom serialization and deserialization simply by calling
```csharp
Binary.RegisterCustomSerializer<CustomType>(writeAction, readAction)
```

All registrations must be done before instantiating the Binary class.  In order for custom serialization to be used, the SupportSerializationHooks property on the Settings used to instantiate the Binary class must be set to true.

Both the write Action and read Action will be called with an instance of the type being serialized/deserialized and a BinaryWriter/BinaryReader interface which exposes three methods:

```csharp
        void Write(string input);
        void Write<T>(T value) where T : struct;
        void WriteObject<T>(T value);
```

The Actions can optionally take a third parameter for context, which is set on the Binary instance with SetCustomHookContext.

The reader has corresponding methods for reading back the values.  Behavior of the generic Write/Read method when passed a non-primitive is undefined.  If multiple customer serializers match an object, they will all be called in the order in which they were registered.

#### Tips for best performance

- Use sealed type declarations when possible - this allows the serializer to skip writing any type information
- Create empty constructors (or constructors that assign to every field from parameters matching the field types) for classes that will be serialized/deserialized a lot (only helps if there's no inline field initialization as well)
- Use different serializer instances for different workloads (e.g. one for serializing a few objects at a time and one for large graphs)
- Don't inherit from standard collections
