namespace Askyl.Dsm.WebHosting.Data;

public interface IGenericCloneable<T> where T : class, new()
{
    public T Clone();
}
