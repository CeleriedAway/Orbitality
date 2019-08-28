using UnityEngine;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;

namespace Game
{
    public class GameView : ConnectableObject
    {
        [SerializeField] Transform root;
        [SerializeField] Transform targetingArrow;
        [SerializeField] Transform hudCanvas;
        
        public void Show(IReactiveCollection<Planet> planets, IReactiveCollection<RocketInstance> rockets, ICell<int> playerPlanetId)
        {
            // Present function is a very powerful tool
            // It allows to show any dynamic collection it can:
            //     * Automate loading and destruction
            //     * Automate object pooling
            //     * Show UI data with different layouts
            //     * Show UI data in scroll views with reusable cells (when only visible elements are loaded)
            //     * some other usefull features ...
            // Here is the simplest case of Present
            planets.Present(
                root, 
                prefabSelector: data => Resources.Load<PlanetView>(data.config.name),
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

        public void SetHudCanvasVisible(ICell<bool> visible) => hudCanvas.SetActive(visible);

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