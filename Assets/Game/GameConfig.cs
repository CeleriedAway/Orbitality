using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "config", menuName = "Create Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        static GameConfig _instance;
        public static GameConfig instance => _instance ?? (_instance = Resources.Load<GameConfig>("config"));
        
        public List<RocketConfig> rockets;
    }
}