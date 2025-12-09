using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEditor.UIElements;

[assembly: UxmlNamespacePrefix("AdvancedSceneManager.UI", "asm")]
[assembly: InternalsVisibleTo("AdvancedSceneManager.Tests")]
[assembly: InternalsVisibleTo("AssetDashboard")]
[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Keeps warning for callbacks.")]
