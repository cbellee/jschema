﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.JSchema.Tests;
using Xunit;

namespace Microsoft.JSchema.Generator.Tests
{
    public class DataModelGeneratorTests
    {
        private const string PrimaryOutputFilePath = TestFileSystem.OutputDirectory + "\\" + TestSettings.RootClassName + ".cs";
        private const string CopyrightFilePath = @"C:\copyright.txt";

        private TestFileSystem _testFileSystem;
        private readonly DataModelGeneratorSettings _settings;

        public DataModelGeneratorTests()
        {
            _testFileSystem = new TestFileSystem();
            _settings = TestSettings.MakeSettings();
        }

        [Fact(DisplayName = "DataModelGenerator throws if output directory exists")]
        public void ThrowsIfOutputDirectoryExists()
        {
            _settings.ForceOverwrite = false;

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(new JsonSchema());

            // ... and the message should mention the output directory.
            action.ShouldThrow<JSchemaException>().WithMessage($"*{TestFileSystem.OutputDirectory}*");
        }

        [Fact(DisplayName = "DataModelGenerator does not throw if output directory does not exist")]
        public void DoesNotThrowIfOutputDirectoryDoesNotExist()
        {
            // Use a directory name other than the default. The mock file system believes
            // that only the default directory exists.
            _settings.OutputDirectory = _settings.OutputDirectory + "x";

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Basic");

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldNotThrow();
        }

        [Fact(DisplayName = "DataModelGenerator does not throw if ForceOverwrite setting is set")]
        public void DoesNotThowIfForceOverwriteSettingIsSet()
        {
            // This is the default from MakeSettings; restated here for explicitness.
            _settings.ForceOverwrite = true;

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Basic");

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldNotThrow();
        }

        [Fact(DisplayName = "DataModelGenerator throws if copyright file does not exist")]
        public void ThrowsIfCopyrightFileDoesNotExist()
        {
            _settings.CopyrightFilePath = CopyrightFilePath;

            _testFileSystem.Mock.Setup(fs => fs.FileExists(CopyrightFilePath))
                .Returns(false);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Basic");

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            // ... and the exception message should mention the file path.
            action.ShouldThrow<JSchemaException>().WithMessage($"*{CopyrightFilePath}*");
        }

        [Fact(DisplayName = "DataModelGenerator throws if root schema is not of type 'object'")]
        public void ThrowsIfRootSchemaIsNotOfTypeObject()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("NotAnObject");

            Action action = () => generator.Generate(schema);

            // ... and the message should mention what the root type actually was.
            action.ShouldThrow<JSchemaException>().WithMessage("*number*");
        }

        [Fact(DisplayName = "DataModelGenerator generates class description")]
        public void GeneratesClassDescription()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Basic");

            string actual = generator.Generate(schema);

            const string Expected =
@"namespace N
{
    /// <summary>
    /// The description
    /// </summary>
    public partial class C
    {
    }
}";
            actual.Should().Be(Expected);

            // This particular test shows not only that the correct text was produced,
            // but that it was written to the expected path. Subsequent tests will
            // just verify the text.
            _testFileSystem.Files.Should().OnlyContain(key => key.Equals(PrimaryOutputFilePath));
        }

        [Fact(DisplayName = "DataModelGenerator generates properties with built-in types")]
        public void GeneratesPropertiesWithBuiltInTypes()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Properties");

            string actual = generator.Generate(schema);

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public string StringProp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double NumberProp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool BooleanProp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int IntegerProp { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates object-valued property with correct type")]
        public void GeneratesObjectValuedPropertyWithCorrectType()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Object");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public D ObjectProp { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator throws if reference is not a fragment")]
        public void ThrowsIfReferenceIsNotAFragment()
        {
            const string SchemaText = @"
{
  ""type"": ""object"",
  ""properties"": {
    ""p"": {
      ""$ref"": ""https://example.com/pschema.schema.json/#""
    }
  },
}";
            JsonSchema schema = SchemaReader.ReadSchema(SchemaText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldThrow<JSchemaException>()
                .WithMessage("*https://example.com/pschema.schema.json/#*");
        }

        [Fact(DisplayName = "DataModelGenerator throws if reference does not specify a definition")]
        public void ThrowsIfReferenceDoesNotSpecifyADefinition()
        {
            const string SchemaText = @"
{
  ""type"": ""object"",
  ""properties"": {
    ""p"": {
      ""$ref"": ""#/notDefinitions/p""
    }
  },
  ""notDefinitions"": {
    ""p"": {
    }
  }
}";
            JsonSchema schema = SchemaReader.ReadSchema(SchemaText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldThrow<JSchemaException>()
                .WithMessage("*#/notDefinitions/p*");
        }

        [Fact(DisplayName = "Throws if referenced definition does not exist")]
        public void ThrowsIfReferencedDefinitionDoesNotExist()
        {
            const string SchemaText = @"
{
  ""type"": ""object"",
  ""properties"": {
    ""p"": {
      ""$ref"": ""#/definitions/nonExistentDefinition""
    }
  },
  ""definitions"": {
    ""p"": {
    }
  }
}";
            JsonSchema schema = SchemaReader.ReadSchema(SchemaText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldThrow<JSchemaException>()
                .WithMessage("*nonExistentDefinition*");
        }

        [Fact(DisplayName = "DataModelGenerator generates array-valued property")]
        public void GeneratesArrayValuedProperty()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Array");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public object[] ArrayProp { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates XML comments for properties")]
        public void GeneratesXmlCommentsForProperties()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("PropertyDescription");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// An example property.
        /// </summary>
        public string ExampleProp { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";

            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates XML comments for properties whose property type is ref")]
        public void GeneratesXmlCommentsForPropertiesWhosePropertyTypeIsRef()
        {
            _settings.RootClassName = "consoleWindow";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);


            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""description"": ""Describes a console window."",
  ""properties"": {
    ""foregroundColor"": {
      ""$ref"": ""#/definitions/color"",
      ""description"": ""The color of the text on the screen.""
    },
    ""backgroundColor"": {
      ""$ref"": ""#/definitions/color"",
      ""description"": ""The color of the screen background.""
    },
  },
  ""definitions"": {
    ""color"": {
      ""type"": ""object"",
      ""description"": ""Describes a color with R, G, and B components."",
      ""properties"": {
        ""red"": {
          ""type"": ""integer"",
          ""description"": ""The value of the R component.""
        },
        ""green"": {
          ""type"": ""integer"",
          ""description"": ""The value of the G component.""
        },
        ""blue"": {
          ""type"": ""integer"",
          ""description"": ""The value of the B component.""
        }
      }
    }
  }
}");

            const string RootClassText =
@"namespace N
{
    /// <summary>
    /// Describes a console window.
    /// </summary>
    public partial class ConsoleWindow
    {
        /// <summary>
        /// The color of the text on the screen.
        /// </summary>
        public Color ForegroundColor { get; set; }

        /// <summary>
        /// The color of the screen background.
        /// </summary>
        public Color BackgroundColor { get; set; }

        public bool Equals(ConsoleWindow other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";

            const string ColorClassText =
@"namespace N
{
    /// <summary>
    /// Describes a color with R, G, and B components.
    /// </summary>
    public partial class Color
    {
        /// <summary>
        /// The value of the R component.
        /// </summary>
        public int Red { get; set; }

        /// <summary>
        /// The value of the G component.
        /// </summary>
        public int Green { get; set; }

        /// <summary>
        /// The value of the B component.
        /// </summary>
        public int Blue { get; set; }

        public bool Equals(Color other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";

            generator.Generate(schema);

            string rootFilePath = TestFileSystem.MakeOutputFilePath(_settings.RootClassName.ToPascalCase());
            string colorFilePath = TestFileSystem.MakeOutputFilePath("Color");

            var expectedOutputFiles = new List<string>
            {
                rootFilePath,
                colorFilePath
            };

            _testFileSystem.Files.Count.Should().Be(expectedOutputFiles.Count);
            _testFileSystem.Files.Should().OnlyContain(key => expectedOutputFiles.Contains(key));

            _testFileSystem[rootFilePath].Should().Be(RootClassText);
            _testFileSystem[colorFilePath].Should().Be(ColorClassText);
        }

        [Fact(DisplayName = "DataModelGenerator generates copyright notice")]
        public void GeneratesCopyrightAtTopOfFile()
        {
            const string CopyrightNotice =
@"// Copyright (c) 2016. All rights reserved.
// Licensed under Apache 2.0 license.
";
            _settings.CopyrightFilePath = CopyrightFilePath;

            _testFileSystem.Mock.Setup(fs => fs.FileExists(CopyrightFilePath))
                .Returns(true);
            _testFileSystem.Mock.Setup(fs => fs.ReadAllText(CopyrightFilePath))
                .Returns(CopyrightNotice);

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("PropertyDescription");

            const string Expected =
@"// Copyright (c) 2016. All rights reserved.
// Licensed under Apache 2.0 license.

namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// An example property.
        /// </summary>
        public string ExampleProp { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates classes for schemas in definitions")]
        public void GeneratesClassesForSchemasInDefinitions()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Definitions");

            generator.Generate(schema);

            const string ExpectedRootClass =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public bool RootProp { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";

            const string ExpectedDefinedClass1 =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Def1
    {
        /// <summary>
        /// 
        /// </summary>
        public string Prop1 { get; set; }

        public bool Equals(Def1 other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";

            const string ExpectedDefinedClass2 =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Def2
    {
        /// <summary>
        /// 
        /// </summary>
        public int Prop2 { get; set; }

        public bool Equals(Def2 other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            string def1Path = TestFileSystem.MakeOutputFilePath("Def1");
            string def2Path = TestFileSystem.MakeOutputFilePath("Def2");

            var expectedOutputFiles = new List<string>
            {
                PrimaryOutputFilePath,
                def1Path,
                def2Path
            };

            _testFileSystem.Files.Count.Should().Be(expectedOutputFiles.Count);
            _testFileSystem.Files.Should().OnlyContain(path => expectedOutputFiles.Contains(path));

            _testFileSystem[PrimaryOutputFilePath].Should().Be(ExpectedRootClass);
            _testFileSystem[def1Path].Should().Be(ExpectedDefinedClass1);
            _testFileSystem[def2Path].Should().Be(ExpectedDefinedClass2);
        }

        [Fact(DisplayName = "DataModelGenerator generates date-time-valued properties")]
        public void GeneratesDateTimeValuedProperties()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
  ""startTime"": {
    ""type"": ""string"",
    ""format"": ""date-time""
    }
  }
}");

            const string Expected =
@"using System;

namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public DateTime StartTime { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates uri-valued properties")]
        public void GeneratesUriValuedProperties()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
  ""targetFile"": {
    ""type"": ""string"",
    ""format"": ""uri""
    }
  }
}");

            const string Expected =
@"using System;

namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public Uri TargetFile { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates integer property from dictionary reference")]
        public void GeneratesIntegerPropertyFromDictionaryReference()
        {
            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""intDefProp"": {
      ""$ref"": ""#/definitions/d""
    },
  },
  ""definitions"": {
    ""d"": {
      ""type"": ""integer""
    }
  }
}");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public int IntDefProp { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of primitive types by $ref")]
        public void GeneratesArrayOfPrimitiveTypeByReference()
        {
            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfIntByRef"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/d""
      }
    }
  },
  ""definitions"": {
    ""d"": {
      ""type"": ""integer"",
      }
    }
  }
}");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public int[] ArrayOfIntByRef { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of arrays of primitive type")]
        public void GeneratesArrayOfArraysOfPrimitiveType()
        {
            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfArrayOfInt"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/itemType""
      }
    }
  },
  ""definitions"": {
    ""itemType"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""integer""
      }
    }
  }
}");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public int[][] ArrayOfArrayOfInt { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of arrays of object type")]
        public void GeneratesArrayOfArraysOfObjectType()
        {
            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfArrayOfObject"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/itemType""
      }
    }
  },
  ""definitions"": {
    ""itemType"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object""
      }
    }
  }
}");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public object[][] ArrayOfArrayOfObject { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of arrays of class type")]
        public void GeneratesArrayOfArraysOfClassType()
        {
            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfArrayOfD"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/itemType""
      }
    }
  },
  ""definitions"": {
    ""itemType"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/d""
      }
    },
    ""d"": {
      ""type"": ""object"",
      }
    }
  }
}");

            const string Expected =
@"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public D[][] ArrayOfArrayOfD { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates string for inline enum of string")]
        public void GeneratesStringForInlineEnumOfString()
        {
            JsonSchema schema = SchemaReader.ReadSchema(
    @"{
  ""type"": ""object"",
  ""properties"": {
    ""version"": {
      ""enum"": [
        ""v1.0"",
        ""v2.0""
      ]
    }
  }
}");

            const string Expected =
    @"namespace N
{
    /// <summary>
    /// 
    /// </summary>
    public partial class C
    {
        /// <summary>
        /// 
        /// </summary>
        public string Version { get; set; }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }
        }
    }
}";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }
    }
}
