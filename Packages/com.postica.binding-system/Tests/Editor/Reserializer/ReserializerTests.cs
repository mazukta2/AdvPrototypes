using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Postica.BindingSystem.Serialization;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Postica.BindingSystem.Tests
{
    public class ReserializerTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void RegexReplacePrefabInstanceArrayPropertyDirect_Passes()
        {
            var input = Strings.WithBind_PrefabInstance_Array;

            var desiredOutput = Strings.NoBind_PrefabInstance_Array;

            var fromString = "- target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}\n      propertyPath: list.Array[#]._value\n      value:";
            var toString = "- target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}\n      propertyPath: list.Array.data[$1]\n      value:";

            var pattern = fromString.Replace(@".Array[#]", @"\.Array\.data\[(\d+)\]");
            var regex = new Regex(pattern, RegexOptions.Multiline);

            // Act
            Debug.Log($"Replace using pattern:\n {pattern}");
            var output = regex.Replace(input, toString);

            // Assert
            Debug.Log($"Output:\n{output}");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void RegexReplacePrefabInstanceArrayPropertyField_Passes()
        {
            var input = Strings.WithBind_PrefabInstance_Array;

            var desiredOutput = Strings.NoBind_PrefabInstance_Array;

            var from = string.Format(Reserializer.Yaml.prefabPropertyTemplate, "324483512837101895", "e21ede4abb696ca4a9b51d0b3fcf2a7f", "list.Array[#]._value");
            var to = string.Format(Reserializer.Yaml.prefabPropertyTemplate, "324483512837101895", "e21ede4abb696ca4a9b51d0b3fcf2a7f", "list.Array[#]");

            // Act
            Debug.Log($"Replace from:\n{from}\n\nto:\n{to}\n\n");
            var output = Reserializer.RegEx.ReplaceArrayField(input, from, to);

            // Assert
            Debug.Log($"Output:\n{output}");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void SubstringTillNextIndent_List_Passes()
        {
            var input = Strings.WithBind_PrefabInstance_Array;

            var desiredOutput = @" {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: bla.y.y
      value: 1.44
      objectReference: {fileID: 0}
".Replace("\r", "");

            var startIndex = input.IndexOf("target:") + "target:".Length;
            var sb = new StringBuilder(input);

            // Act
            var output = Reserializer.GetSubstringTillNextIndent(2, sb, startIndex);

            // Assert
            Debug.Log($"Output:\n{output}");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void SubstringTillNextIndent_Simple_Passes()
        {
            var input = Strings.WithBind_Asset_NoArray;

            var desiredOutput = @"
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
".Replace("\r", "");

            var startIndex = input.IndexOf("vs:") + "vs:".Length;
            var sb = new StringBuilder(input);

            // Act
            var output = Reserializer.GetSubstringTillNextIndent(1, sb, startIndex);

            // Assert
            Debug.Log($"Output:\n{output}");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void GetIndent_Simple_Passes()
        {
            var input = Strings.WithBind_Asset_NoArray;

            var desiredOutput1 = 1;
            var desiredOutput2 = 2;
            var desiredOutput3 = 3;

            var sb = new StringBuilder(input);

            var startIndex1 = input.IndexOf("vs:") + "vs:".Length;
            var startIndex2 = input.IndexOf("_bindData:") + "_bindData:".Length;
            var startIndex3 = input.IndexOf("y: {x: 2, y:") + "y: {x: 2, y:".Length;

            // Act
            var output1 = Reserializer.GetIndent(sb, startIndex1);
            var output2 = Reserializer.GetIndent(sb, startIndex2);
            var output3 = Reserializer.GetIndent(sb, startIndex3);

            // Assert
            Assert.AreEqual(desiredOutput1, output1, "Indent 1");
            Assert.AreEqual(desiredOutput2, output2, "Indent 2");
            Assert.AreEqual(desiredOutput3, output3, "Indent 3");
        }

        [Test]
        public void GetIndent_List_Passes()
        {
            var input = Strings.WithBind_Asset_List;

            var desiredOutput1 = 1;
            var desiredOutput2 = 2;
            var desiredOutput3 = 3;

            var sb = new StringBuilder(input);

            var startIndex1 = input.IndexOf("list:") + "vs:".Length;
            var startIndex2 = input.IndexOf("_bindData:", startIndex1) + "_bindData:".Length;
            var startIndex3 = input.IndexOf("Source", startIndex2) + "Source".Length;

            // Act
            var output1 = Reserializer.GetIndent(sb, startIndex1, true);
            var output2 = Reserializer.GetIndent(sb, startIndex2, true);
            var output3 = Reserializer.GetIndent(sb, startIndex3, true);

            // Assert
            Assert.AreEqual(desiredOutput1, output1, "Indent 1");
            Assert.AreEqual(desiredOutput2, output2, "Indent 2");
            Assert.AreEqual(desiredOutput3, output3, "Indent 3");
        }

        [Test]
        public void AddIndent_Positive_Passes()
        {
            var input = Strings.WithBind_Asset_NoArray;

            var desiredOutput = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
  bla:
      _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value:
        x: 1
        y: {x: 2, y: 3}
  list:
  - _bindData:".Replace("\r", "");

            var startIndex = input.IndexOf("bla:") + "bla:".Length;
            var sb = new StringBuilder(input);

            // Act
            var outputDelta = Reserializer.AddIndent(sb, 1, 1, startIndex, true, false);
            var output = sb.ToString();

            // Assert
            Debug.Log($"Output:\n{output}");
            Assert.IsTrue(outputDelta > 0);
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void AddIndent_Negative_Passes()
        {
            var input = Strings.WithBind_Asset_NoArray;

            var desiredOutput = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
  bla:
  _bindData:
    Source: {fileID: 0}
    Path: 
    _mode: 0
    _parameters: []
    _mainParamIndex: 0
    _readConverter:
      rid: -2
    _writeConverter:
      rid: -2
    _modifiers: []
    _sourceType: 
    _ppath: 
    _flags: 0
  _isBound: 0
  _value:
    x: 1
    y: {x: 2, y: 3}
  list:
  - _bindData:".Replace("\r", "");

            var startIndex = input.IndexOf("bla:") + "bla:".Length;
            var sb = new StringBuilder(input);

            // Act
            var outputDelta = Reserializer.AddIndent(sb, 1, -1, startIndex, true, false);
            var output = sb.ToString();

            // Assert
            Debug.Log($"Output:\n{output}");
            Assert.IsTrue(outputDelta < 0);
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void RegexReplaceStringBuilder_Simple_Passes()
        {
            var input = Strings.WithBind_Asset_NoArray;

            var desiredOutput = Strings.NoBind_Asset_Simple_NoArray;

            var startIndex = input.IndexOf("vs:") + "vs:".Length;
            var sb = new StringBuilder(input);
            var regex = Reserializer.RegEx.bindDataRegex;
            var deltaIndex = desiredOutput.Length - input.Length;

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outputDeltaIndex = Reserializer.RegexReplace(sb, regex, "", 1, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.AreEqual(deltaIndex, outputDeltaIndex, "Delta Index");
            Assert.AreEqual(desiredOutput, output);
        }


        [Test]
        public void ProcessField_Simple_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_Simple_NoArray;

            var desiredOutput = Strings.WithBind_Asset_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "vs",
                isCompound = false,
                oldPath = "vs",
                path = "vs._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_Simple_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_NoArray;

            var desiredOutput = Strings.NoBind_Asset_Simple_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "vs",
                isCompound = false,
                oldPath = "vs._value",
                path = "vs"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_Compound_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_NoArray;

            var desiredOutput = Strings.NoBind_Asset_Compound_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "bla",
                isCompound = true,
                oldPath = "bla._value",
                path = "bla"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_Compound_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_Compound_NoArray;

            var desiredOutput = Strings.WithBind_Asset_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "bla",
                isCompound = true,
                oldPath = "bla",
                path = "bla._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_List_Simple_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_Simple_List;

            var desiredOutput = Strings.WithBind_Asset_List;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "list.Array[#]",
                isCompound = false,
                oldPath = "list.Array[#]",
                path = "list.Array[#]._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_List_Simple_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_List;

            var desiredOutput = Strings.NoBind_Asset_Simple_List;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "list.Array[#]",
                isCompound = false,
                oldPath = "list.Array[#]._value",
                path = "list.Array[#]"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_List_Compound_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_List;

            var desiredOutput = Strings.NoBind_Asset_Compound_List;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "binds.Array[#]",
                isCompound = true,
                oldPath = "binds.Array[#]._value",
                path = "binds.Array[#]"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_List_Compound_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_Compound_List;

            var desiredOutput = Strings.WithBind_Asset_List;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "binds.Array[#]",
                isCompound = true,
                oldPath = "binds.Array[#]",
                path = "binds.Array[#]._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_BindList_Simple_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_BindListBind;

            var desiredOutput = Strings.NoBind_Asset_BindListBind;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "listOfFloats.Array[#]",
                isCompound = false,
                oldPath = "listOfFloats._value.Array[#]._value",
                path = "listOfFloats._value.Array[#]"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_BindList_Simple_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_BindListBind;

            var desiredOutput = Strings.WithBind_Asset_BindListBind;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "listOfFloats.Array[#]",
                isCompound = false,
                oldPath = "listOfFloats._value.Array[#]",
                path = "binds._value.Array[#]._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_BindList_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_BindList;

            var desiredOutput = Strings.NoBind_Asset_BindList;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "listOfFloats",
                isCompound = false,
                isArray = true,
                oldPath = "listOfFloats._value",
                path = "listOfFloats"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_BindList_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_BindList;

            var desiredOutput = Strings.WithBind_Asset_BindList;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "listOfFloats",
                isCompound = false,
                isArray = true,
                oldPath = "listOfFloats",
                path = "binds._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_Compound_Compound_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_Compound_Compound_NoArray;

            var desiredOutput = Strings.NoBind_Asset_Compound_Compound_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "compound.y",
                isCompound = true,
                oldPath = "compound.y._value",
                path = "compound.y"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_Compound_Compound_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_Compound_Compound_NoArray;

            var desiredOutput = Strings.WithBind_Asset_Compound_Compound_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "compound.y",
                isCompound = true,
                oldPath = "compound.y",
                path = "compound.y._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_Full_Compound_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_Compound_Compound_NoArray;

            var desiredOutput = Strings.NoBind_Asset_Full_Compound_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field1 = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "compound.y",
                isCompound = true,
                oldPath = "compound._value.y._value",
                path = "compound.y"
            };

            var field2 = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "compound",
                isCompound = true,
                oldPath = "compound._value",
                path = "compound"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome1 = Reserializer.ProcessField(field1, sb, startIndex);
            sw.Stop();

            var output1 = sb.ToString();
            var elapsed1 = sw.ElapsedMilliseconds;

            sw = new Stopwatch();
            sw.Start();
            var outcome2 = Reserializer.ProcessField(field2, sb, startIndex);
            sw.Stop();
            var output2 = sb.ToString();

            // Assert
            Debug.Log($"Output 1 [{elapsed1} ms]:\n{output1}");
            Debug.Log($"Output 2 [{sw.ElapsedMilliseconds} ms]:\n{output2}");
            Assert.IsTrue(outcome1, "Outcome 1");
            Assert.IsTrue(outcome2, "Outcome 2");
            Assert.AreEqual(desiredOutput, output2);
        }

        [Test]
        public void ProcessField_Full_Compound_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_Full_Compound_NoArray;

            var desiredOutput = Strings.WithBind_Asset_Compound_Compound_NoArray;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field1 = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "compound.y",
                isCompound = true,
                oldPath = "compound.y",
                path = "compound._value.y._value"
            };

            var field2 = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "compound",
                isCompound = true,
                oldPath = "compound",
                path = "compound._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome1 = Reserializer.ProcessField(field1, sb, startIndex);
            sw.Stop();

            var output1 = sb.ToString();
            var elapsed1 = sw.ElapsedMilliseconds;

            sw = new Stopwatch();
            sw.Start();
            var outcome2 = Reserializer.ProcessField(field2, sb, startIndex);
            sw.Stop();
            var output2 = sb.ToString();

            // Assert
            Debug.Log($"Output 1 [{elapsed1} ms]:\n{output1}");
            Debug.Log($"Output 2 [{sw.ElapsedMilliseconds} ms]:\n{output2}");
            Assert.IsTrue(outcome1, "Outcome 1");
            Assert.IsTrue(outcome2, "Outcome 2");
            Assert.AreEqual(desiredOutput, output2);
        }

        [Test]
        public void ProcessField_EmptyList_FromBind_Passes()
        {
            var input = Strings.NoBind_Asset_Empty_List;

            var desiredOutput = Strings.NoBind_Asset_Empty_List;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "empty.Array[#]",
                isCompound = true,
                oldPath = "empty.Array[#]._value",
                path = "empty.Array[#]"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_EmptyList_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_Empty_List;

            var desiredOutput = Strings.NoBind_Asset_Empty_List;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "empty.Array[#]",
                isCompound = true,
                oldPath = "empty.Array[#]",
                path = "empty.Array[#]._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_PrimitiveList_Int32_FromBind_Passes()
        {
            var input = Strings.WithBind_Asset_PrimitiveList;

            var desiredOutput = Strings.NoBind_Asset_PrimitiveList;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.FromBind,
                id = "arrayList.Array[#]",
                isCompound = false,
                primitive = nameof(Int32),
                oldPath = "arrayList.Array[#]._value",
                path = "arrayList.Array[#]"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }

        [Test]
        public void ProcessField_PrimitiveList_Int32_ToBind_Passes()
        {
            var input = Strings.NoBind_Asset_PrimitiveList;

            var desiredOutput = Strings.WithBind_Asset_PrimitiveList;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var field = new BindDatabase.DeltaBindField()
            {
                change = BindDatabase.DeltaBindField.Change.ToBind,
                id = "arrayList.Array[#]",
                isCompound = false,
                primitive = nameof(Int32),
                oldPath = "arrayList.Array[#]",
                path = "arrayList.Array[#]._value"
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            var outcome = Reserializer.ProcessField(field, sb, startIndex);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.IsTrue(outcome, "Outcome");
            Assert.AreEqual(desiredOutput, output);
        }



        [Test]
        public void ProcessFields_FullAsset_FromBind_Passes()
        {
            var input = Strings.WithBind_FullAsset;

            var desiredOutput = Strings.NoBind_FullAsset;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var errorsSb = new StringBuilder();
            var fields = JsonUtility.FromJson<BindDatabase.DeltaDatabase>(Strings.BindDelta_FromBind);
            var deltaTypes = 1;

            // Act
            var sw = new Stopwatch();
            sw.Start();
            Reserializer.UpgradeTextBlock(fields.types[0], startIndex, sb, errorsSb);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.AreEqual(deltaTypes, fields.types.Count, "Number of delta types");
            Assert.AreEqual(desiredOutput, output, "The output does not match");
            Assert.IsTrue(errorsSb.Length == 0, "Errors Content is not Empty");
        }

        [Test]
        public void ProcessFields_FullAsset_ToBind_Passes()
        {
            var input = Strings.NoBind_FullAsset;

            var desiredOutput = Strings.WithBind_FullAsset;

            var startIndex = input.IndexOf("m_EditorClassIdentifier:") + "m_EditorClassIdentifier:".Length;
            var sb = new StringBuilder(input);
            var errorsSb = new StringBuilder();
            var fields = JsonUtility.FromJson<BindDatabase.DeltaDatabase>(Strings.BindDelta_ToBind);
            var deltaTypes = 1;

            // Act
            var sw = new Stopwatch();
            sw.Start();
            Reserializer.UpgradeTextBlock(fields.types[0], startIndex, sb, errorsSb);
            sw.Stop();

            var output = sb.ToString();

            // Assert
            Debug.Log($"Output [{sw.ElapsedMilliseconds} ms]:\n{output}");
            Assert.AreEqual(deltaTypes, fields.types.Count, "Number of delta types");
            Assert.AreEqual(desiredOutput, output, "The output does not match");
            Assert.IsTrue(errorsSb.Length == 0, "Errors Content is not Empty");
        }






        private class Strings
        {
            public static readonly string WithBind_PrefabInstance_Array = @"objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: bla.y.y
      value: 1.44
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: vs._value
      value: 9.93
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: list.Array.data[0]._value
      value: 9
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: list.Array.data[1]._value
      value: 10
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: list.Array.data[2]._".Replace("\r", "");

            public static readonly string WithBind_Asset_NoArray = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:".Replace("\r", "");

            public static readonly string WithBind_Asset_Compound_Compound_NoArray = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
  bla:
    x: 1
    y: {x: 2, y: 3}
  compound:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y:
        _bindData:
          Source: {fileID: 0}
          Path: 
          _mode: 0
          _parameters: []
          _mainParamIndex: 0
          _readConverter:
            rid: -2
          _writeConverter:
            rid: -2
          _modifiers: []
          _sourceType: 
          _ppath: 
          _flags: 0
        _isBound: 0
        _value: {x: 0, y: 0}
  list:
  - _bindData:".Replace("\r", "");

            public static readonly string WithBind_Asset_List = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  arrayList: 010000000200000003000000
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 33
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 333
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 444
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 4
    - 6
    - 8
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 9, y: 10}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 11
      y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string WithBind_Asset_BindListBind = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 33
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 333
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 444
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: 4
    - _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: 6
    - _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: 8
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 9, y: 10}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 11
      y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string WithBind_Asset_BindList = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 33
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 333
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 444
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 4
    - 6
    - 8
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 9, y: 10}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 11
      y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string WithBind_Asset_PrimitiveList = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  arrayList:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 1
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 2
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 13
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: ".Replace("\r", "");

            public static readonly string NoBind_PrefabInstance_Array = @"objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: bla.y.y
      value: 1.44
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: vs._value
      value: 9.93
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: list.Array.data[0]
      value: 9
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: list.Array.data[1]
      value: 10
      objectReference: {fileID: 0}
    - target: {fileID: 324483512837101895, guid: e21ede4abb696ca4a9b51d0b3fcf2a7f, type: 3}
      propertyPath: list.Array.data[2]._".Replace("\r", "");

            public static readonly string NoBind_Asset_Simple_NoArray = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs: 42
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_Compound_NoArray = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
  bla:
    x: 1
    y: {x: 2, y: 3}
  list:
  - _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_Compound_Compound_NoArray = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
  bla:
    x: 1
    y: {x: 2, y: 3}
  compound:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 0, y: 0}
  list:
  - _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_Full_Compound_NoArray = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 42
  bla:
    x: 1
    y: {x: 2, y: 3}
  compound:
    x: 8
    y: {x: 0, y: 0}
  list:
  - _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_Simple_List = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  arrayList: 010000000200000003000000
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - 5.5
  - 33
  - 333
  - 444
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 4
    - 6
    - 8
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 9, y: 10}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 11
      y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_Empty_List = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  arrayList: 010000000200000003000000
  empty: []
  list:
  - 5.5
  - 33
  - 333
  - 444
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 4
    - 6
    - 8
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 9, y: 10}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 11
      y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_Compound_List = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  arrayList: 010000000200000003000000
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 33
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 333
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 444
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 4
    - 6
    - 8
  binds:
  - x: 8
    y: {x: 9, y: 10}
  - x: 11
    y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_BindListBind = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 33
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 333
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 444
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 4
    - 6
    - 8
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 9, y: 10}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 11
      y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_BindList = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 33
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 333
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 444
  listOfFloats:
  - 4
  - 6
  - 8
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 8
      y: {x: 9, y: 10}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 11
      y: {x: 12, y: 13}
  sceneGo:
    _bindData:".Replace("\r", "");

            public static readonly string NoBind_Asset_PrimitiveList = @"m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  arrayList: 01000000020000000D000000
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 1
      y: {x: 2, y: 3}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5.5
  - _bindData:
      Source: {fileID: 0}
      Path: ".Replace("\r", "");


            public static readonly string BindDelta_ToBind = @"{
    ""types"": [
        {
            ""type"": ""Postica.FlowUI.UpgradeTest, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",
            ""guid"": ""3eb051902ccbdd94db7b14f25258fba9"",
            ""localId"": ""11500000"",
            ""fields"": [
                {
                    ""id"": ""vs"",
                    ""path"": ""vs._value"",
                    ""oldPath"": ""vs"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""bla"",
                    ""path"": ""bla._value"",
                    ""oldPath"": ""bla"",
                    ""isCompound"": true,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""asset"",
                    ""path"": ""asset._value"",
                    ""oldPath"": ""asset"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""prefab"",
                    ""path"": ""prefab._value"",
                    ""oldPath"": ""prefab"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""sceneGo"",
                    ""path"": ""sceneGo._value"",
                    ""oldPath"": ""sceneGo"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""listOfFloats"",
                    ""path"": ""listOfFloats._value"",
                    ""oldPath"": ""listOfFloats"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": true,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""list.Array[#]"",
                    ""path"": ""list.Array[#]._value"",
                    ""oldPath"": ""list.Array[#]"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""binds.Array[#]"",
                    ""path"": ""binds.Array[#]._value"",
                    ""oldPath"": ""binds.Array[#]"",
                    ""isCompound"": true,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""sceneComponent"",
                    ""path"": ""sceneComponent"",
                    ""oldPath"": ""sceneComponent._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""prefabComponent"",
                    ""path"": ""prefabComponent._value"",
                    ""oldPath"": ""prefabComponent"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""boolList.Array[#]"",
                    ""path"": ""boolList.Array[#]._value"",
                    ""oldPath"": ""boolList.Array[#]"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Boolean"",
                    ""change"": 0
                },
                {
                    ""id"": ""boolAray.Array[#]"",
                    ""path"": ""boolAray.Array[#]._value"",
                    ""oldPath"": ""boolAray.Array[#]"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Boolean"",
                    ""change"": 0
                },
                {
                    ""id"": ""charAray.Array[#]"",
                    ""path"": ""charAray.Array[#]._value"",
                    ""oldPath"": ""charAray.Array[#]"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Char"",
                    ""change"": 0
                },
                {
                    ""id"": ""byteAray.Array[#]"",
                    ""path"": ""byteAray.Array[#]._value"",
                    ""oldPath"": ""byteAray.Array[#]"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Byte"",
                    ""change"": 0
                },
                {
                    ""id"": ""arrayList.Array[#]"",
                    ""path"": ""arrayList.Array[#]._value"",
                    ""oldPath"": ""arrayList.Array[#]"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Int32"",
                    ""change"": 0
                }
            ]
        }
    ]
}";

            public static readonly string BindDelta_FromBind = @"{
    ""types"": [
        {
            ""type"": ""Postica.FlowUI.UpgradeTest, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",
            ""guid"": ""3eb051902ccbdd94db7b14f25258fba9"",
            ""localId"": ""11500000"",
            ""fields"": [
                {
                    ""id"": ""vs"",
                    ""path"": ""vs"",
                    ""oldPath"": ""vs._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""bla"",
                    ""path"": ""bla"",
                    ""oldPath"": ""bla._value"",
                    ""isCompound"": true,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""asset"",
                    ""path"": ""asset"",
                    ""oldPath"": ""asset._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""prefab"",
                    ""path"": ""prefab"",
                    ""oldPath"": ""prefab._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""sceneGo"",
                    ""path"": ""sceneGo"",
                    ""oldPath"": ""sceneGo._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""listOfFloats"",
                    ""path"": ""listOfFloats"",
                    ""oldPath"": ""listOfFloats._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": true,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""list.Array[#]"",
                    ""path"": ""list.Array[#]"",
                    ""oldPath"": ""list.Array[#]._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""binds.Array[#]"",
                    ""path"": ""binds.Array[#]"",
                    ""oldPath"": ""binds.Array[#]._value"",
                    ""isCompound"": true,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""sceneComponent"",
                    ""path"": ""sceneComponent._value"",
                    ""oldPath"": ""sceneComponent"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 0
                },
                {
                    ""id"": ""prefabComponent"",
                    ""path"": ""prefabComponent"",
                    ""oldPath"": ""prefabComponent._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": """",
                    ""change"": 1
                },
                {
                    ""id"": ""boolList.Array[#]"",
                    ""path"": ""boolList.Array[#]"",
                    ""oldPath"": ""boolList.Array[#]._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Boolean"",
                    ""change"": 1
                },
                {
                    ""id"": ""boolAray.Array[#]"",
                    ""path"": ""boolAray.Array[#]"",
                    ""oldPath"": ""boolAray.Array[#]._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Boolean"",
                    ""change"": 1
                },
                {
                    ""id"": ""charAray.Array[#]"",
                    ""path"": ""charAray.Array[#]"",
                    ""oldPath"": ""charAray.Array[#]._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Char"",
                    ""change"": 1
                },
                {
                    ""id"": ""byteAray.Array[#]"",
                    ""path"": ""byteAray.Array[#]"",
                    ""oldPath"": ""byteAray.Array[#]._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Byte"",
                    ""change"": 1
                },
                {
                    ""id"": ""arrayList.Array[#]"",
                    ""path"": ""arrayList.Array[#]"",
                    ""oldPath"": ""arrayList.Array[#]._value"",
                    ""isCompound"": false,
                    ""isReference"": false,
                    ""isArray"": false,
                    ""primitive"": ""Int32"",
                    ""change"": 1
                }
            ]
        }
    ]
}";

            public static readonly string WithBind_FullAsset_Faulted = @"  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 1
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    x: 2
    y:
      _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: {x: 3, y: 4.56}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 6
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 7
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 8
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 9.9
    - 10.1
    - 11
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: x: 12
    y:
      _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: {x: 13, y: 14.4}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: x: 15
    y:
      _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: {x: 16.78, y: 17.89}
  arrayList: 0a00000064000000
  boolList: 0100
  sceneGo:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 8523912328553323710, guid: 3a49188106fe8c146886297873580d3f, type: 3}
  sceneComponent: {fileID: 2569268495263171124, guid: 6fe6f8fc23b4a944d9993cd468f6a052, type: 3}
  prefab:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 9191008967447357898, guid: 2c9f12153c7354b48832894565ac6b76, type: 3}
  prefabComponent:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 2298047735656903551, guid: ede1920fdbf96864f92beb1247ea0a31, type: 3}
  asset:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 2800000, guid: a85eaf61c8210a940b43d33aa780eb66, type: 3}
  boolAray: []
  charAray: []
  byteAray: []
  ushortArray: 0b00803e4000".Replace("\r", "");

            public static readonly string WithBind_FullAsset = @"  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 1
  bla:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 2
      y:
        _bindData:
          Source: {fileID: 0}
          Path: 
          _mode: 0
          _parameters: []
          _mainParamIndex: 0
          _readConverter:
            rid: -2
          _writeConverter:
            rid: -2
          _modifiers: []
          _sourceType: 
          _ppath: 
          _flags: 0
        _isBound: 0
        _value: {x: 3, y: 4.56}
  list:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 5
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 6
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 7
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 8
  listOfFloats:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
    - 9.9
    - 10.1
    - 11
  binds:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 12
      y:
        _bindData:
          Source: {fileID: 0}
          Path: 
          _mode: 0
          _parameters: []
          _mainParamIndex: 0
          _readConverter:
            rid: -2
          _writeConverter:
            rid: -2
          _modifiers: []
          _sourceType: 
          _ppath: 
          _flags: 0
        _isBound: 0
        _value: {x: 13, y: 14.4}
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value:
      x: 15
      y:
        _bindData:
          Source: {fileID: 0}
          Path: 
          _mode: 0
          _parameters: []
          _mainParamIndex: 0
          _readConverter:
            rid: -2
          _writeConverter:
            rid: -2
          _modifiers: []
          _sourceType: 
          _ppath: 
          _flags: 0
        _isBound: 0
        _value: {x: 16.78, y: 17.89}
  arrayList:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 10
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 100
  boolList:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 1
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  sceneGo:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 8523912328553323710, guid: 3a49188106fe8c146886297873580d3f, type: 3}
  sceneComponent: {fileID: 2569268495263171124, guid: 6fe6f8fc23b4a944d9993cd468f6a052, type: 3}
  prefab:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 9191008967447357898, guid: 2c9f12153c7354b48832894565ac6b76, type: 3}
  prefabComponent:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 2298047735656903551, guid: ede1920fdbf96864f92beb1247ea0a31, type: 3}
  asset:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 2800000, guid: a85eaf61c8210a940b43d33aa780eb66, type: 3}
  boolAray:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 0
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 1
  charAray:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 97
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 65
  byteAray:
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 64
  - _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: 255
  ushortArray: 0b00803e4000".Replace("\r", "");

            public static readonly string NoBind_FullAsset = @"  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3eb051902ccbdd94db7b14f25258fba9, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  vs: 1
  bla:
    x: 2
    y:
      _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: {x: 3, y: 4.56}
  list:
  - 5
  - 6
  - 7
  - 8
  listOfFloats:
  - 9.9
  - 10.1
  - 11
  binds:
  - x: 12
    y:
      _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: {x: 13, y: 14.4}
  - x: 15
    y:
      _bindData:
        Source: {fileID: 0}
        Path: 
        _mode: 0
        _parameters: []
        _mainParamIndex: 0
        _readConverter:
          rid: -2
        _writeConverter:
          rid: -2
        _modifiers: []
        _sourceType: 
        _ppath: 
        _flags: 0
      _isBound: 0
      _value: {x: 16.78, y: 17.89}
  arrayList: 0A00000064000000
  boolList: 0100
  sceneGo: {fileID: 8523912328553323710, guid: 3a49188106fe8c146886297873580d3f, type: 3}
  sceneComponent:
    _bindData:
      Source: {fileID: 0}
      Path: 
      _mode: 0
      _parameters: []
      _mainParamIndex: 0
      _readConverter:
        rid: -2
      _writeConverter:
        rid: -2
      _modifiers: []
      _sourceType: 
      _ppath: 
      _flags: 0
    _isBound: 0
    _value: {fileID: 2569268495263171124, guid: 6fe6f8fc23b4a944d9993cd468f6a052, type: 3}
  prefab: {fileID: 9191008967447357898, guid: 2c9f12153c7354b48832894565ac6b76, type: 3}
  prefabComponent: {fileID: 2298047735656903551, guid: ede1920fdbf96864f92beb1247ea0a31, type: 3}
  asset: {fileID: 2800000, guid: a85eaf61c8210a940b43d33aa780eb66, type: 3}
  boolAray: 0001
  charAray: 61004100
  byteAray: 40ff
  ushortArray: 0b00803e4000".Replace("\r", "");
        }

    }
}