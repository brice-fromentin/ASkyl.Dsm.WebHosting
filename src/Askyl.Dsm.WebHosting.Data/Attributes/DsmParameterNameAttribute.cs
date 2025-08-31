namespace Askyl.Dsm.WebHosting.Data.Attributes;

[System.AttributeUsage(AttributeTargets.Class)]
public class DsmParameterNameAttribute (string name) : System.Attribute
{
    public string Name { get; } = name;
}
