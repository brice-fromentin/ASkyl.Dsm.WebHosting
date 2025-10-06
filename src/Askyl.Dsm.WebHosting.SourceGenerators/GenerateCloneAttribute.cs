namespace Askyl.Dsm.WebHosting.SourceGenerators;

/// <summary>
/// Marks a class for automatic Clone() method generation.
/// The class must implement IGenericCloneable&lt;T&gt; and be declared as partial.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateCloneAttribute : System.Attribute
{
}
