using UnityEngine;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;

namespace Game
{
    public class GameView : ConnectableObject
    {
        public Transform root;

        public void Show(Cell<GameModel> model)
        {
            model.MapWithDefaultIfNull(m => m.planets, StaticCollection<Planet>.Empty()).Join().Present(root, 
                prefabSelector: data => PrefabRef<PlanetView>.ByPrefab(Resources.Load<PlanetView>(data.config.name)),
                show: (data, view) => view.Show(data, model.value.playerPlanetId == data.id));
            
            model.MapWithDefaultIfNull(m => m.rockets, StaticCollection<RocketInstance>.Empty()).Join().Present(root, 
                prefabSelector: data => PrefabRef<RocketView>.ByName("Missile" + data.config.name),
                show: (data, view) => view.Show(data));
        }
    }
}