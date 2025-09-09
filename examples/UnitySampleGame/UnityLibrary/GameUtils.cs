using System.Collections.Generic;

namespace UnityLibrary
{
    public static class GameUtils
    {
        public static T GetRandomElement<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            var random = new System.Random();
            return list[random.Next(list.Count)];
        }
        
        public static void ShuffleList<T>(List<T> list)
        {
            var random = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
        
        public static string FormatScore(int score)
        {
            if (score >= 1000000) return (score / 1000000f).ToString("F1") + "M";
            if (score >= 1000) return (score / 1000f).ToString("F1") + "K";
            return score.ToString();
        }
    }
}

