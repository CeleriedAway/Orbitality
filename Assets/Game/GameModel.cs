using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using TWL;
using UnityEngine;
using ZergRush;
using ZergRush.ReactiveCore;

namespace Game
{
    [Serializable]
    public class PlanetConfig
    {
        public string name;
        public float gravityPower;
        public float orbitDistance;
        public float radius;
        // I decided to do non realistic rotation speed that does not depend on orbit
        public float rotationSpeed;
        public float maxHp;

        public bool destructable => maxHp > 0;
    }

    // I often design game entities as public clean data
    [Serializable]
    public class Planet
    {
        [CanBeNull] public RocketConfig rocketUsed;
        [CanBeNull] public PlanetConfig config;

        public float rotationPhase;
        // data that is rarely changed i wrap into reactive property 
        public float hp;
        public Vector2 position;
        public float currentRocketCooldown;
        public bool onCooldown => currentRocketCooldown > 0;
        public float relativeCooldown => currentRocketCooldown / rocketUsed.cooldown;
        public int id;
        
        // In some simple cases its ok to store view pointer
        // But its makes a little less convenient code, so often this is not a production case
        [GenIgnore] public PlanetView currentView;

    }
    
    [Serializable]
    public class RocketConfig
    {
        public string name;
        public float startingSpeed;
        public float acceleration;
        public float damage;
        public float cooldown;
        public float gravityInfluence = 1;
        public string description;
        // to prevent rocket collision with its own planet
        public float invulTime = 0.5f;
    }

    public class RocketInstance
    {
        [CanBeNull]
        public RocketConfig config;
        public Vector2 position;
        public Vector2 speed;
        public float invulTime;
        public float lifeTime;
        public int parentId;
    }

    public enum GameResult
    {
        Undefined,
        Win,
        Loss
    }

    [GenTask(GenTaskFlags.JsonSerialization | GenTaskFlags.DefaultConstructor)]
    public partial class GameModel : IJsonSerializable
    {
        // This is reactive collection tool, its make easier to present it with views, to show with different settings
        // May be it is not the most powerful application, but its useful enough
        public ReactiveCollection<RocketInstance> rockets;
        public ReactiveCollection<Planet> planets;

        public int playerPlanetId;

        public Planet PlanetWithId(int id) => planets.FirstOrDefault(p => p.id == id);

        public bool IsGameFinished(out GameResult result)
        {
            if (PlanetWithId(playerPlanetId) == null)
            {
                result = GameResult.Loss;
                return true;
            }
            // player and the sun
            else if (planets.Count == 2)
            {
                result = GameResult.Win;
                return true;
            }
            result = GameResult.Undefined;
            return false;
        }

        public void Shoot(Planet planet, Vector2 direction)
        {
            if (planet.currentRocketCooldown > 0) Debug.LogError("actually can't shoot at the moment");

            planet.currentRocketCooldown = planet.rocketUsed.cooldown;
            direction.Normalize();
            var rocket = planet.rocketUsed;
            rockets.Add(new RocketInstance
            {
                config = rocket,
                position = planet.position + direction * planet.config.radius,
                speed =  rocket.startingSpeed * direction,
                invulTime = rocket.invulTime,
                lifeTime = 15,
                parentId = planet.id
            });
        }

        public void Update(float dt)
        {
            UpdatePhysicsAndLogic(dt);
            UpdateCollisions();
        }

        // I decided to go with custom physics because it is much cleaner, easier to save/load
        // and game logic is so simple it is not required complex physics engine at all
        void UpdatePhysicsAndLogic(float dt)
        {
            for (var i = rockets.Count - 1; i >= 0; i--)
            {
                var rocket = rockets[i];
                rocket.lifeTime -= dt;
                if (rocket.lifeTime <= 0) rockets.RemoveAt(i);
                
                rocket.speed += dt * rocket.config.gravityInfluence * GravityField(rocket.position);
                // Native rocket acceleration
                rocket.speed += rocket.speed.normalized * (rocket.config.acceleration * dt);
                rocket.position += rocket.speed * dt;
                rocket.invulTime -= dt;
            }

            foreach (var planet in planets)
            {
                planet.rotationPhase += dt * planet.config.rotationSpeed;
                planet.position = planet.config.orbitDistance *
                                  new Vector2(Mathf.Cos(planet.rotationPhase), 0.7f * Mathf.Sin(planet.rotationPhase));
                planet.currentRocketCooldown -= dt;
            }
        }

        // n^2 cause data volumes are too small
        void UpdateCollisions()
        {
            // removing entities right at the place is or with reverse iteration
            // it can be also done with some 'destroyed' flags with later iteration
            for (var i = rockets.Count - 1; i >= 0; i--)
            {
                var rocket = rockets[i];
                for (var j = planets.Count - 1; j >= 0; j--)
                {
                    var planet = planets[j];
                    // rocket owner is unvul to its own rockets for some time
                    if (rocket.invulTime > 0 && rocket.parentId == planet.id) continue;
                    if ((rocket.position - planet.position).magnitude <= planet.config.radius)
                    {
                        if (planet.config.destructable)
                        {
                            planet.hp -= rocket.config.damage;
                            if (planet.hp <= 0)
                            {
                                planets.RemoveAt(j);
                            }
                        }
                        rockets.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        // I actually does not need mass because actual acceleration does not depend on it
        Vector2 GravityField(Vector2 pos)
        {
            Vector2 field = Vector2.zero;
            foreach (var planet in planets)
            {
                var dir = planet.position - pos;
                var dist = dir.magnitude;
                dir.Normalize();
                field += dir * (planet.config.gravityPower / (dist * dist));
            }
            return field;
        }

        // ok.. some hardcoded rockets
    }

    public static class Tools
    {
    }
}