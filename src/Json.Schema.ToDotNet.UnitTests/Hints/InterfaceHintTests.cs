﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Json.Schema.ToDotNet.UnitTests;
using Xunit;

namespace Microsoft.Json.Schema.ToDotNet.Hints.UnitTests
{
    public class InterfaceHintTests
    {
        private readonly TestFileSystem _testFileSystem;
        private readonly DataModelGeneratorSettings _settings;

        public InterfaceHintTests()
        {
            _testFileSystem = new TestFileSystem();
            _settings = TestSettings.MakeSettings();
        }

        public static readonly object[] TestCases = new object[]
        {
            // We give the
            new object[]
            {
@"{
  ""type"": ""object"",
  ""description"": ""My class with an interface."",
  ""properties"": {
    ""value"": {
      ""description"": ""The value."",
      ""type"": ""integer""
    }
  }
}",

@"{
  ""c"": [
    {
      ""kind"": ""InterfaceHint"",
      ""arguments"": {
        ""description"": ""My interface.""
      }
    }
  ]
}",

@"using System;
using System.CodeDom.Compiler;
using System.Runtime.Serialization;

namespace N
{
    /// <summary>
    /// My class with an interface.
    /// </summary>
    [DataContract]
    [GeneratedCode(""Microsoft.Json.Schema.ToDotNet"", """ + VersionConstants.FileVersion + @""")]
    public partial class C : IC, IEquatable<C>
    {
        /// <summary>
        /// The value.
        /// </summary>
        [DataMember(Name = ""value"", IsRequired = false, EmitDefaultValue = false)]
        public int Value { get; set; }
    }
}",

@"using System.CodeDom.Compiler;

namespace N
{
    /// <summary>
    /// My interface.
    /// </summary>
    [GeneratedCode(""Microsoft.Json.Schema.ToDotNet"", """ + VersionConstants.FileVersion + @""")]
    public interface IC
    {
        /// <summary>
        /// The value.
        /// </summary>
        int Value { get; }
    }
}"
            }
        };

        [Theory(DisplayName = "InterfaceHint generates interfaces in addition to classes")]
        [MemberData(nameof(TestCases))]
        public void GeneratesInterfaceFromClass(
            string schemaText,
            string hintsText,
            string classText,
            string interfaceText)
        {
            _settings.HintDictionary = new HintDictionary(hintsText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(schemaText);

            generator.Generate(schema);

            string primaryOutputFilePath = TestFileSystem.MakeOutputFilePath(_settings.RootClassName);
            string interfaceFilePath = TestFileSystem.MakeOutputFilePath("I" + _settings.RootClassName);

            var expectedOutputFiles = new List<string>
            {
                primaryOutputFilePath,
                interfaceFilePath
            };

            _testFileSystem.Files.Count.Should().Be(expectedOutputFiles.Count);
            _testFileSystem.Files.Should().OnlyContain(key => expectedOutputFiles.Contains(key));

            _testFileSystem[primaryOutputFilePath].Should().Be(classText);
            _testFileSystem[interfaceFilePath].Should().Be(interfaceText);
        }
    }
}
