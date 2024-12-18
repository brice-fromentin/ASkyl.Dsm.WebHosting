namespace Askyl.Dsm.WebHosting.Data.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DsmParameterNameAttribute (string name) : Attribute
{
    public string Name { get; } = name;
}
