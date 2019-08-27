using UnityEngine;

namespace Game
{
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