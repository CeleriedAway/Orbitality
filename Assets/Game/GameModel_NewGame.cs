using TWL;
using ZergRush;

namespace Game
{
    // All generic highly usable resources are here

    public partial class GameModel
    {
        static ZergRandom random = new ZergRandom();
        static PlanetConfig Sun = new PlanetConfig {name = "Sun", gravityPower = 1, orbitDistance = 0, radius = 1};

        static Planet NewPlanet(PlanetConfig config, int id)
        {
            return new Planet
            {
                config = config,
                id = id,
                rotationPhase = (float) random.NextDouble() * 10,
                rocketUsed = GameConfig.instance.rockets.RandomElement(random),
                hp = config.maxHp,
                currentRocketCooldown = random.NextFloat()
            };
        }

        static string PlanetPath(string category, int number) => $"{category}/{category}_Design{number}";

        static string[] PlanetNames = new string[]
            {PlanetPath("Frozen", 1), PlanetPath("Earth-like", 1), PlanetPath("Alien", 1)};

        public static GameModel New(RocketConfig playerRocket)
        {
            var model = new GameModel();

            // sun as planet impl )) right now it seems ok
            model.planets.Add(NewPlanet(Sun, 0));
            int planetCount = random.Range(5, 5);
            float basePlanetOrbit = 1;
            for (int i = 1; i <= planetCount; i++)
            {
                var orbit = 0.5f + basePlanetOrbit * i;
                var config = new PlanetConfig
                {
                    radius = random.Range(0.4f, 0.5f),
                    rotationSpeed = random.Sign() * 5 * random.Range(0.1f, 0.16f) / (orbit),
                    name = PlanetNames.RandomElement(random),
                    gravityPower = 1,
                    maxHp = 50,
                    orbitDistance = orbit
                };
                model.planets.Add(NewPlanet(config, i));
            }
            model.playerPlanetId = random.Range(1, planetCount + 1);
            if (playerRocket != null)
                model.PlanetWithId(model.playerPlanetId).rocketUsed = playerRocket;
            model.Update(0); // settings planet positions
            return model;
        }
    }
}