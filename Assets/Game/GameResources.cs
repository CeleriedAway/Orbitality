using UnityEngine;

namespace Game
{
    // All highly usable 100% need to load prefabs and sprites I found better to organize this way,
    // rather then call Resources.Load every time
    [CreateAssetMenu(fileName = "resources", menuName = "Create Game Resources", order = 1)]
    public class GameResources : ScriptableObject
    {
        static GameResources _instance;
        public static GameResources instance => _instance ?? (_instance = Resources.Load<GameResources>("resources"));

        public Transform planetDeathFx;
        public Transform rocketExplosionFx;
        public HpBarCanvas hpBar;
    }
}