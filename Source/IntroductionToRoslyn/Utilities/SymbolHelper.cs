using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntroductionToRoslyn.Utilities
{
    public static class SymbolHelper
    {
		public static async Task<CompilationUnitSyntax> GetCSharpSyntaxRootAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			return (CompilationUnitSyntax)root;
		}

		public static bool DerivesFrom(ITypeSymbol currentType, ITypeSymbol expectedType)
		{
			if (currentType == null || expectedType == null)
			{
				return false;
			}

			if (currentType.Equals(expectedType))
			{
				return true;
			}

			if (expectedType.SpecialType == SpecialType.System_Object
				&& currentType.IsReferenceType)
			{
				return true;
			}

			if (expectedType.TypeKind == TypeKind.Interface)
			{
				// Check if it implements the interface...
				var interfaceSymbol = currentType.AllInterfaces.FirstOrDefault(i => i.MetadataName == expectedType.MetadataName);

				if (interfaceSymbol != null)
				{
					return true;
				}
			}

			return DerivesFrom(currentType, expectedType.ToString());
		}

		public static bool DerivesFrom(ITypeSymbol currentType, string expectedMetaType)
		{
			if (currentType == null)
			{
				return false;
			}

			bool derives = false;
			var type = currentType;
			while (type != null)
			{
				if (type.ToString() == expectedMetaType)
				{
					derives = true;
					break;
				}

				// Check if it implements the interface...
				var interfaceSymbol = type.Interfaces.FirstOrDefault(i => i.ToString() == expectedMetaType);

				if (interfaceSymbol != null)
				{
					derives = true;
					break;
				}

				type = type.BaseType;
			}

			return derives;
		}
    }
}
