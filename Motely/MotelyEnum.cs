
namespace Motely;

public static class MotelyEnum<T> where T : unmanaged, Enum
{
    public static readonly int ValueCount = Enum.GetValues<T>().Length;
    public static readonly T[] Values = Enum.GetValues<T>();
}