﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Saltarelle.Compiler.ScriptSemantics;

namespace Saltarelle.Compiler.Tests.ScriptSharpMetadataImporter {
	[TestFixture]
	public class TypeTests : ScriptSharpMetadataImporterTestBase {
		[Test]
		public void TopLevelClassWithoutAttributesWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;

namespace TestNamespace {
	public class SomeType {
	}
}");
			var type = FindType("TestNamespace.SomeType");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("TestNamespace.SomeType"));
		}

		[Test]
		public void NestedClassWithoutAttributesWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;

namespace TestNamespace {
	public class Outer {
		public class SomeType {
		}
	}
}");
			var type = FindType("TestNamespace.Outer+SomeType");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("TestNamespace.Outer$SomeType"));
		}

		[Test]
		public void MultipleNestingWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;

namespace TestNamespace {
	public class Outer {
		public class Inner {
			public class SomeType {
			}
		}
	}
}");
			var type = FindType("TestNamespace.Outer+Inner+SomeType");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("TestNamespace.Outer$Inner$SomeType"));
		}

		[Test]
		public void ScriptNameAttributeCanChangeTheNameOfATopLevelClass() {
			Prepare(
@"using System.Runtime.CompilerServices;

namespace TestNamespace {
	[ScriptName(""Renamed"")]
	public class SomeType {
	}
}");

			var type = FindType("TestNamespace.SomeType");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("TestNamespace.Renamed"));
		}

		[Test]
		public void ScriptNameAttributeCanChangeTheNameOfANestedClass() {
			Prepare(
@"using System.Runtime.CompilerServices;

namespace TestNamespace {
	[ScriptName(""RenamedOuter"")]
	public class Outer {
		[ScriptName(""Renamed"")]
		public class SomeType {
		}
	}
}");
			
			var type = FindType("TestNamespace.Outer+SomeType");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("TestNamespace.Renamed"));
		}

		[Test]
		public void ClassOutsideNamespaceWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;

public class SomeType {
}
");

			var type = FindType("SomeType");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("SomeType"));
		}

		[Test]
		public void ClassOutsideNamespaceWithScriptNameAttributeWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;

[ScriptName(""Renamed"")]
public class SomeType {
}
");

			var type = FindType("SomeType");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("Renamed"));
		}

		[Test]
		public void GenericTypeWithoutScriptNameAttributeWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;

public class SomeType<T1, T2> {
}
");

			var type = FindType("SomeType`2");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("SomeType$2"));
		}

		[Test]
		public void GenericTypeWithScriptNameAttributeWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;

[ScriptName(""Renamed"")]
public class SomeType<T1, T2> {
}
");

			var type = FindType("SomeType`2");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("Renamed"));
		}

		[Test]
		public void MultipleGenericNestedNamesAreCorrect() {
			Prepare(
@"using System.Runtime.CompilerServices;

namespace TestNamespace {
	public class Outer<T1,T2> {
		public class Inner<T3> {
			public class SomeType<T4,T5> {
			}
		}
	}
}");

			var type = FindType("TestNamespace.Outer`2+Inner`1+SomeType`2");
			Assert.That(type.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(type.Name, Is.EqualTo("TestNamespace.Outer$2$Inner$1$SomeType$2"));
		}

		[Test]
		public void TypeNamesAreMinimizedForNonPublicTypesIfTheMinimizeFlagIsSet() {
			Prepare(
@"class C1 {}
internal class C2 {}
public class C3 {}
public class C4 { internal class C5 { public class C6 {} } }
internal class C7 { public class C8 { public class C9 {} } }
public class C10 { private class C11 {} protected class C12 {} protected internal class C13 {} }
");

			Assert.That(FindType("C1").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C2").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C3").Name, Is.EqualTo("C3"));
			Assert.That(FindType("C4").Name, Is.EqualTo("C4"));
			Assert.That(FindType("C4+C5").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C4+C5+C6").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C7").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C7+C8").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C7+C8+C9").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C10+C11").Name, Is.StringMatching("^\\$[0-9]+$"));
			Assert.That(FindType("C10+C12").Name, Is.EqualTo("C10$C12"));
			Assert.That(FindType("C10+C13").Name, Is.EqualTo("C10$C13"));
		}

		[Test]
		public void MinimizedTypeNamesAreUniquePerNamespace() {
			Prepare(
@"class C1 {}
class C2 { class C3 {} }

namespace X {
	class C4 {}
	class C5 { class C6 {} }
}

namespace X.Y {
	class C7 {}
	class C8 { class C9 {} }
}");

			Assert.That(new[] { "C1", "C2", "C2+C3" }.Select(s => FindType(s).Name).ToList(), Is.EquivalentTo(new[] { "$0", "$1", "$2" }));
			Assert.That(new[] { "X.C4", "X.C5", "X.C5+C6" }.Select(s => FindType(s).Name).ToList(), Is.EquivalentTo(new[] { "X.$0", "X.$1", "X.$2" }));
			Assert.That(new[] { "X.Y.C7", "X.Y.C8", "X.Y.C8+C9" }.Select(s => FindType(s).Name).ToList(), Is.EquivalentTo(new[] { "X.Y.$0", "X.Y.$1", "X.Y.$2" }));
		}

		[Test]
		public void ScriptNameAttributePreventsMinimizationOfTypeNames() {
			Prepare(
@"using System.Runtime.CompilerServices;
[ScriptName(""Renamed1"")] class C1 {}
namespace X {
	[ScriptName(""Renamed2"")]
	class C2 {
		[ScriptName(""Renamed3"")]
		class C3 {}
	}
	class C4 {
		[ScriptName(""Renamed5"")]
		class C5 {
		}
	}
}");

			Assert.That(FindType("C1").Name, Is.EqualTo("Renamed1"));
			Assert.That(FindType("X.C2").Name, Is.EqualTo("X.Renamed2"));
			Assert.That(FindType("X.C2+C3").Name, Is.EqualTo("X.Renamed3"));
			Assert.That(FindType("X.C4").Name, Is.EqualTo("X.$0"));
			Assert.That(FindType("X.C4+C5").Name, Is.EqualTo("X.Renamed5"));
		}

		[Test, Ignore("TODO")]
		public void InternalTypesArePrefixedWithADollarSignIfTheMinimizeFlagIsNotSet() {
			Prepare(
@"class C1 {}
internal class C2 {}
public class C3 {}
public class C4 { internal class C5 { public class C6 {} } }
internal class C7 { public class C8 { public class C9 {} } }
public class C10 { private class C11 {} protected class C12 {} protected internal class C13 {} }
", false);

			Assert.That(FindType("C1").Name, Is.EqualTo(""));
			Assert.That(FindType("C2").Name, Is.EqualTo(""));
			Assert.That(FindType("C3").Name, Is.EqualTo("C3"));
			Assert.That(FindType("C4").Name, Is.EqualTo("C4"));
			Assert.That(FindType("C4+C5").Name, Is.EqualTo(""));
			Assert.That(FindType("C4+C5+C6").Name, Is.EqualTo(""));
			Assert.That(FindType("C7").Name, Is.EqualTo(""));
			Assert.That(FindType("C7+C8").Name, Is.EqualTo(""));
			Assert.That(FindType("C7+C8+C9").Name, Is.EqualTo(""));
			Assert.That(FindType("C10+C11").Name, Is.EqualTo(""));
			Assert.That(FindType("C10+C12").Name, Is.EqualTo("C10$C12"));
			Assert.That(FindType("C10+C13").Name, Is.EqualTo("C10$C13"));
		}

		[Test]
		public void ScriptNamespaceAttributeCanBeUsedToChangeNamespaceOfTypes() {
			Prepare(
@"using System.Runtime.CompilerServices;
[ScriptNamespace(""Some.Namespace"")] public class C1 {}
namespace X {
	[ScriptNamespace(""OtherNamespace"")]
	public class C2 {}
}");

			var t1 = FindType("C1");
			Assert.That(t1.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(t1.Name, Is.EqualTo("Some.Namespace.C1"));

			var t2 = FindType("X.C2");
			Assert.That(t2.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(t2.Name, Is.EqualTo("OtherNamespace.C2"));
		}

		[Test]
		public void EmptyScriptNamespaceAttributeWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;
[ScriptNamespace("""")] public class C1 {}
namespace X {
	[ScriptNamespace("""")]
	public class C2 {}
}");

			var t1 = FindType("C1");
			Assert.That(t1.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(t1.Name, Is.EqualTo("C1"));

			var t2 = FindType("X.C2");
			Assert.That(t2.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(t2.Name, Is.EqualTo("C2"));
		}

		[Test]
		public void IgnoreNamespaceAttributeWorks() {
			Prepare(
@"using System.Runtime.CompilerServices;
[IgnoreNamespace] public class C1 {}
namespace X {
	[IgnoreNamespace]
	public class C2 {}
}");

			var t1 = FindType("C1");
			Assert.That(t1.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(t1.Name, Is.EqualTo("C1"));

			var t2 = FindType("X.C2");
			Assert.That(t2.Type, Is.EqualTo(TypeScriptSemantics.ImplType.NormalType));
			Assert.That(t2.Name, Is.EqualTo("C2"));
		}

		[Test]
		public void ScriptNamespaceAttributeCannotBeAppliedToNestedTypes() {
			Prepare(
@"using System.Runtime.CompilerServices;
namespace X {
	public class C1 {
		[ScriptNamespace(""X"")]
		public class C2 {}
	}
}", expectErrors: true);

			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("nested type") && AllErrors[0].Contains("X.C1.C2") && AllErrors[0].Contains("ScriptNamespace"));
		}

		[Test]
		public void IgnoreNamespaceAttributeCannotBeAppliedToNestedTypes() {
			Prepare(
@"using System.Runtime.CompilerServices;
namespace X {
	public class C1 {
		[IgnoreNamespace]
		public class C2 {}
	}
}", expectErrors: true);

			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("nested type") && AllErrors[0].Contains("X.C1.C2") && AllErrors[0].Contains("IgnoreNamespace"));
		}

		[Test]
		public void CannotApplyBothIgnoreNamespaceAndScriptNamespaceToTheSameClass() {
			Prepare(
@"using System.Runtime.CompilerServices;
namespace X {
	[IgnoreNamespace, ScriptNamespace(""X"")]
	public class C1 {
		public class C2 {}
	}
}", expectErrors: true);

			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("X.C1") && AllErrors[0].Contains("IgnoreNamespace") && AllErrors[0].Contains("ScriptNamespace"));
		}

		[Test]
		public void ScriptNameAttributeOnTypeMustBeAValidJSIdentifier() {
			var er = new MockErrorReporter(false);
			Prepare(@"using System.Runtime.CompilerServices; [ScriptName("""")] public class C1 {}", expectErrors: true);
			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("ScriptName") && AllErrors[0].Contains("must be a valid JavaScript identifier"));

			er = new MockErrorReporter(false);
			Prepare(@"using System.Runtime.CompilerServices; [ScriptName(""X.Y"")] public class C1 {}", expectErrors: true);
			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("ScriptName") && AllErrors[0].Contains("must be a valid JavaScript identifier"));

			er = new MockErrorReporter(false);
			Prepare(@"using System.Runtime.CompilerServices; [ScriptName(""a b"")] public class C1 {}", expectErrors: true);
			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("ScriptName") && AllErrors[0].Contains("must be a valid JavaScript identifier"));
		}

		[Test]
		public void ScriptNamespaceAttributeArgumentMustBeAValidJSQualifiedIdentifierOrBeEmpty() {
			var er = new MockErrorReporter(false);
			Prepare(@"using System.Runtime.CompilerServices; [ScriptNamespace(""a b"")] public class C1 {}", expectErrors: true);
			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("ScriptNamespace") && AllErrors[0].Contains("must be a valid JavaScript qualified identifier"));

			er = new MockErrorReporter(false);
			Prepare(@"using System.Runtime.CompilerServices; [ScriptNamespace("" "")] public class C1 {}", expectErrors: true);
			Assert.That(AllErrors, Has.Count.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("ScriptNamespace") && AllErrors[0].Contains("must be a valid JavaScript qualified identifier"));
		}

		[Test]
		public void ScriptNamespaceAndIgnoreNamespaceAttributesAreConsideredWhenMinimizingNames() {
			Prepare(
@"using System.Runtime.CompilerServices;

class C1 {}
[ScriptNamespace(""X"")] class C2 {}
[ScriptNamespace(""X.Y"")] class C3 {}

namespace X {
	[ScriptNamespace("""")] class C4 {}
	class C5 {}
	[ScriptNamespace(""X.Y"")] class C6 {}
}

namespace X.Y {
	[IgnoreNamespace] class C7 {}
	class C8 {}
	[ScriptNamespace(""X"")] class C9 {}
}");

			Assert.That(new[] { "C1", "X.C4", "X.Y.C7" }.Select(s => FindType(s).Name).ToList(), Is.EquivalentTo(new[] { "$0", "$1", "$2" }));
			Assert.That(new[] { "C2", "X.C5", "X.Y.C9" }.Select(s => FindType(s).Name).ToList(), Is.EquivalentTo(new[] { "X.$0", "X.$1", "X.$2" }));
			Assert.That(new[] { "C3", "X.C6", "X.Y.C8" }.Select(s => FindType(s).Name).ToList(), Is.EquivalentTo(new[] { "X.Y.$0", "X.Y.$1", "X.Y.$2" }));
		}

		[Test]
		public void PreserveNameAttributePreventsMinimization() {
			Prepare(
@"using System.Runtime.CompilerServices;
[PreserveName] class C1 {}
[PreserveName] internal class C2 {}
[PreserveName] public class C3 {}
[PreserveName] public class C4 { [PreserveName] internal class C5 { [PreserveName] public class C6 {} } }
[PreserveName] internal class C7 { [PreserveName] public class C8 { [PreserveName] public class C9 {} } }
[PreserveName] public class C10 { [PreserveName] private class C11 {} [PreserveName] protected class C12 {} [PreserveName] protected internal class C13 {} }
");

			Assert.That(FindType("C1").Name, Is.EqualTo("C1"));
			Assert.That(FindType("C2").Name, Is.EqualTo("C2"));
			Assert.That(FindType("C3").Name, Is.EqualTo("C3"));
			Assert.That(FindType("C4").Name, Is.EqualTo("C4"));
			Assert.That(FindType("C4+C5").Name, Is.EqualTo("C4$C5"));
			Assert.That(FindType("C4+C5+C6").Name, Is.EqualTo("C4$C5$C6"));
			Assert.That(FindType("C7").Name, Is.EqualTo("C7"));
			Assert.That(FindType("C7+C8").Name, Is.EqualTo("C7$C8"));
			Assert.That(FindType("C7+C8+C9").Name, Is.EqualTo("C7$C8$C9"));
			Assert.That(FindType("C10+C11").Name, Is.EqualTo("C10$C11"));
			Assert.That(FindType("C10+C12").Name, Is.EqualTo("C10$C12"));
			Assert.That(FindType("C10+C13").Name, Is.EqualTo("C10$C13"));
		}

		[Test]
		public void GlobalMethodsAttributeCausesAllMethodsToBeGlobalAndPreventsMinimization() {
			Prepare(
@"using System.Runtime.CompilerServices;

[GlobalMethods]
static class C1 {
	[PreserveName]
	static void Method1() {
	}

	[PreserveCase]
	static void Method2() {
	}

	[ScriptName(""Renamed"")]
	static void Method3() {
	}

	static void Method4() {
	}
}");

			var m1 = FindMethod("C1.Method1");
			Assert.That(m1.Type, Is.EqualTo(MethodScriptSemantics.ImplType.NormalMethod));
			Assert.That(m1.Name, Is.EqualTo("method1"));
			Assert.That(m1.IsGlobal, Is.True);

			var m2 = FindMethod("C1.Method2");
			Assert.That(m2.Type, Is.EqualTo(MethodScriptSemantics.ImplType.NormalMethod));
			Assert.That(m2.Name, Is.EqualTo("Method2"));
			Assert.That(m2.IsGlobal, Is.True);

			var m3 = FindMethod("C1.Method3");
			Assert.That(m3.Type, Is.EqualTo(MethodScriptSemantics.ImplType.NormalMethod));
			Assert.That(m3.Name, Is.EqualTo("Renamed"));
			Assert.That(m3.IsGlobal, Is.True);

			var m4 = FindMethod("C1.Method4");
			Assert.That(m4.Type, Is.EqualTo(MethodScriptSemantics.ImplType.NormalMethod));
			Assert.That(m4.Name, Is.EqualTo("method4"));
			Assert.That(m4.IsGlobal, Is.True);
		}

		[Test]
		public void FieldOrPropertyOrEventInGlobalMethodsClassGivesAnError() {
			Prepare(@"using System.Runtime.CompilerServices; [GlobalMethods] static class C1 { static int i; }", expectErrors: true);
			Assert.That(AllErrors.Count, Is.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("GlobalMethodsAttribute") && AllErrors[0].Contains("fields"));

			Prepare(@"using System.Runtime.CompilerServices; [GlobalMethods] static class C1 { static event System.EventHandler e; }", expectErrors: true);
			Assert.That(AllErrors.Count, Is.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("GlobalMethodsAttribute") && AllErrors[0].Contains("events"));

			Prepare(@"using System.Runtime.CompilerServices; [GlobalMethods] static class C1 { static int P { get; set; } }", expectErrors: true);
			Assert.That(AllErrors.Count, Is.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("GlobalMethodsAttribute") && AllErrors[0].Contains("properties"));
		}

		[Test]
		public void GlobalMethodsAttributeCannotBeAppliedToNonStaticClass() {
			Prepare(@"using System.Runtime.CompilerServices; [GlobalMethods] class C1 { static int i; }", expectErrors: true);
			Assert.That(AllErrors.Count, Is.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1") && AllErrors[0].Contains("GlobalMethodsAttribute") && AllErrors[0].Contains("must be static"));
		}

		[Test]
		public void GlobalMethodsAttributeCannotBeAppliedToNestedClass() {
			Prepare(@"using System.Runtime.CompilerServices; static class C1 { [GlobalMethods] static class C2 {} }", expectErrors: true);
			Assert.That(AllErrors.Count, Is.EqualTo(1));
			Assert.That(AllErrors[0].Contains("C1.C2") && AllErrors[0].Contains("GlobalMethodsAttribute") && AllErrors[0].Contains("nested"));
		}
	}
}