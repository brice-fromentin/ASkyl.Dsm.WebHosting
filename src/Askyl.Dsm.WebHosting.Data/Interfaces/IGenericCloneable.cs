namespace Askyl.Dsm.WebHosting.Data.Interfaces;

public interface IGenericCloneable<T> where T : class, new()
{
    public T Clone();
}
