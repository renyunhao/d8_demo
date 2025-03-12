// This has a bunch of functions for generating enums in the editor.

using System.Collections.Generic;

namespace AnimCooker
{
	public static class EnumUtils
	{
		private static readonly HashSet<string> m_keywords = new HashSet<string> {
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
			"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
			"enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
			"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
			"long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
			"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
			"short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
			"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
			"virtual", "void", "volatile", "while"
		};

		// This function will return a string containing an enum declaration with the specified parameters.
		// name --> the name of the enum to create
		// values -> the enum values
		// primitive --> byte, int, uint, short, int64, etc (empty string means no type specifier)
		// makeClassSize --> if this is true, an extra line will be added that makes a static class to hold the size.
		// example:
		//    print(MakeEnumDeclaration("MyType", { Option1, Option2, Option3 }, "byte", true));
		//    output -->  public enum MyType : byte { Option1, Option2, Option3 }
		//                public static class MyTypeSize { public const byte Size = 3; }
		public static string MakeEnumDeclaration(string name, List<string> values, string primitive, bool makeSizeClass)
		{
			string prim = primitive.Length <= 0 ? "" : " : " + primitive;
			string declaration = "public enum " + name + prim + " { ";
			int countMinusOne = values.Count - 1;
			for (int i = 0; i < values.Count; i++) {
				declaration += MakeStringEnumCompatible(values[i]);
				if (i < countMinusOne) { declaration += ", "; }
			}
			declaration += " }\n";
			if (makeSizeClass) {
				declaration += $"public static class {name}Size {{ public const {primitive} Size = {values.Count}; }}\n";
			}
			return declaration;
		}

		public static void WriteDeclarationToFile(string fileName, string declaration, bool reImport = false, string filePath = "Assets/Scripts/Generated/")
		{
			// ensure that the output directory exists
			System.IO.Directory.CreateDirectory(filePath);
			// write the file
			System.IO.File.WriteAllText(filePath + fileName, "// This file was auto-generated\n\n" + declaration);
			#if UNITY_EDITOR
				if (reImport) { UnityEditor.AssetDatabase.ImportAsset(filePath); }
			#endif
		}

		public static void WriteDeclarationsToFile(string fileName, List<string> declarations, bool reImport = false, string filePath = "Assets/Scripts/Generated/")
		{
			string text = "";
			for (int i = 0; i < declarations.Count; i++) { text += declarations[i]; }
			WriteDeclarationToFile(fileName, text, reImport, filePath);
		}

		// given a string, attempts to make the string compatible with an enum
		// if there are any spaces, it will attempt to make the string camel-case
		public static string MakeStringEnumCompatible(string text)
		{
			if (text.Length <= 0) { return "INVALID_ENUM_NAME"; }
			string ret = "";

			// first char must be a letter or an underscore, so ignore anything that is not
			if (char.IsLetter(text[0]) || (text[0] == '_')) { ret += text[0]; }

			// ignore anything that's not a digit or underscore
			bool enableCapitalizeNextLetter = false;
			for (int i = 1; i < text.Length; ++i) {
				if (char.IsLetterOrDigit(text[i]) || (text[i] == '_')) {
					if (enableCapitalizeNextLetter) {
						ret += char.ToUpper(text[i]);
					} else {
						ret += text[i];
					}
					enableCapitalizeNextLetter = false;
				} else if (char.IsWhiteSpace(text[i])) {
					enableCapitalizeNextLetter = true;
				}
			}
			if (ret.Length <= 0) { return "INVALID_ENUM_NAME"; }

			// all the keywords are lowercase, so if we just change the first letter to uppercase,
			// then there will be no conflict
			if (m_keywords.Contains(ret)) { ret = char.ToUpper(ret[0]) + ret.Substring(1); }

			return ret;
		}

		public static bool IsEnumCompatible(string text)
		{
			if (text.Length <= 0) { return false; }

			// first char must be a letter or an underscore, so ignore anything that is not
			if (!(char.IsLetter(text[0]) || (text[0] == '_'))) { return false; }

			// ensure there are no whitespaces
			for (int i = 1; i < text.Length; ++i) {
				if (char.IsWhiteSpace(text[i])) { return false; }
			}
			if (m_keywords.Contains(text)) { return false; }
			return true;
		}
	}
} // namespace