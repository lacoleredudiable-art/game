namespace Dovus.Core.Targeting
{
    public static class LockOnDefaults
    {
        /// <summary>
        /// O-5.18: boss'un başı bundan yüksekteyse kilit bakış noktası baş yerine gövdedir (ÖNERİ, Kül Ejderi faz 2).
        /// </summary>
        public const float HighHeadThresholdM = 10f;

        // Kilit alma / bırakma mesafesi kaynakta yok (ölçüm); çağıran taraf ayardan verir, burada uydurulmaz.
    }
}
