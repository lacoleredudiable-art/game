#if !NET5_0_OR_GREATER
// Unity 6 C# 9'u destekliyor ama .NET Standard 2.1 BCL'inde bu tip yok; onsuz her `init`
// accessor CS0518 ile DERLENMEZ (Unity el kitabı, "C# compiler and language version").
// Çözüm resmî olarak tipi projede tanımlamak. Test projesi net8.0 olduğu için orada
// BCL'deki hâli kullanılır, bu dosya devre dışı kalır.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#endif
