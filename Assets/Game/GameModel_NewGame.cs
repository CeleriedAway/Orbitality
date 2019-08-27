using TWL;
using ZergRush;

namespace Game
{
    public partial class GameModel
    {
        static ZergRandom random = new ZergRandom();
        static PlanetConfig Sun = new PlanetConfig {name = "Sun", gravityPower = 2, orbitDistance = 0, radius = 1};

        static RocketConfig[] rocketConfigs = new[]
        {
            new RocketConfig {acceleration = -0.1f, startingSpeed = 3, damage = 1, cooldown = 2, name = "ShipKiller"},
        };

        static Planet NewPlanet(PlanetConfig config, int id)
        {
            return new Planet
            {
                config = config,
                id = id,
                rotationPhase = (float) random.NextDouble() * 10,
                rocketUsed = rocketConfigs.RandomElement(random),
                hp = config.maxHp,
            };
        }

        static string PlanetPath(string category, int number) => $"{category}/{category}_Design{number}";

        static string[] PlanetNames = new string[]
            {PlanetPath("Frozen", 1), PlanetPath("Earth-like", 1), PlanetPath("Alien", 1)};

        public static GameModel New()
        {
            var model = new GameModel();

            // sun as planet impl )) right now it seems ok
            model.planets.Add(NewPlanet(Sun, 0));
            int planetCount = random.Range(4, 8);
            float basePlanetOrbit = 1;
            for (int i = 1; i <= planetCount; i++)
            {
                var orbit = basePlanetOrbit * i;
                var config = new PlanetConfig
                {
                    radius = random.Range(0.4f, 0.5f),
                    rotationSpeed = 5 * random.Range(0.1f, 0.16f) / (orbit),
                    name = PlanetNames.RandomElement(random),
                    gravityPower = 1,
                    maxHp = 10,
                    orbitDistance = orbit
                };
                model.planets.Add(NewPlanet(config, i));
            }
            model.playerPlanetId = random.Range(1, planetCount + 1);
            model.Update(1); // settings planet positions
            return model;
        }
    }
}