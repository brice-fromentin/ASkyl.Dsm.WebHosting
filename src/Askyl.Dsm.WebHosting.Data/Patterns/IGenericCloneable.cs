namespace Askyl.Dsm.WebHosting.Data.Patterns;

public interface IGenericCloneable<T> where T : class, new()
{
    public T Clone();
}
