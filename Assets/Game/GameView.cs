using UnityEngine;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;

namespace Game
{
    public class GameView : ConnectableObject
    {
        [SerializeField] Transform root;
        [SerializeField] Transform targetingArrow;
        
        public void Show(IReactiveCollection<Planet> planets, IReactiveCollection<RocketInstance> rockets, ICell<int> playerPlanetId)
        {
            planets.Present(
                root, 
                prefabSelector: data => PrefabRef<PlanetView>.ByPrefab(Resources.Load<PlanetView>(data.config.name)),
                show: (data, view) => view.Show(data, playerPlanetId.value == data.id),
                delegates: ExplosionOnDeathDelegates<PlanetView>(GameResources.instance.planetDeathFx)
            );

            rockets.Present(
                root,
                prefabSelector: data => PrefabRef<RocketView>.ByName("Missile" + data.config.name),
                show: (data, view) => view.Show(data),
                delegates: ExplosionOnDeathDelegates<RocketView>(GameResources.instance.rocketExplosionFx)
            );
        }

        public void UpdateTargetingArrow(Vector3 pos, Quaternion rot)
        {
            targetingArrow.SetPositionAndRotation(pos, rot);
        }

        TableDelegates<T> ExplosionOnDeathDelegates<T>(Transform explosionEffectPrefab) where T : ReusableView
        {
            return new TableDelegates<T>
            {
                onRemove = view => {
                    var exp = Instantiate(explosionEffectPrefab, view.transform.position, Quaternion.identity);
                    Destroy(exp.gameObject, 2f);
                    return 0.0f;
                },
            };
        }
    }
}