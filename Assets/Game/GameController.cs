using System;
using UnityEngine;
using ZergRush;
using ZergRush.ReactiveCore;

namespace Game
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] MainMenu menu;
        [SerializeField] GameView view;
        [SerializeField] Camera gameCamera;

        public GameModel game => gameCell.value;
        readonly Cell<GameModel> gameCell = new Cell<GameModel>();
        readonly Cell<bool> paused = new Cell<bool>();

        static string saveFileName = "saveData";

        // I could categorize logic in different functions by theme (loading, view bindings...) 
        // but code amount is so few I do not think that is required
        public void Start()
        {
            gameCell.value = Utils.LoadFromJsonFile<GameModel>(saveFileName, () => null, false);
            
            // game starts paused
            paused.value = true;
            
            // This is reactive binding, so menu will be active when game paused,
            // This approach allows to write cleaner, easier to change code
            menu.SetActive(paused);
            menu.newGame.Subscribe(() =>
            {
                gameCell.value = GameModel.New();
                paused.value = false;
            });
            // Also a reactive binding shortcut, check its readability
            menu.resume.SetActive(gameCell.IsNot(null));
            menu.resume.Subscribe(() => paused.value = false);
            menu.exit.Subscribe(Application.Quit);
            
            // show call 
            view.Show(gameCell);
        }

        public void OnApplicationQuit()
        {
            if (gameCell.value == null) FileWrapper.RemoveIfExists(saveFileName);
            else gameCell.value.SaveToJsonFile(saveFileName);
        }

        void FinishGame(GameResult result)
        {
            gameCell.value = null;
            paused.value = true;
            menu.title.text = $"You {result}!";
        }

        public void Update()
        {
            if (gameCell.value == null || paused.value) return;
            gameCell.value.Update(Time.deltaTime);
            
            // Test hotkeys
            if (Input.GetKeyDown(KeyCode.K)) 
            {
                FinishGame(GameResult.Loss);
                return;
            }

            // Test hotkeys
            if (Input.GetKeyDown(KeyCode.O))
            {
                FinishGame(GameResult.Win);
                return;
            }

            if (gameCell.value.IsGameFinished(out var result))
            {
                FinishGame(result);
                return;
            }
            
            // Input and AI
            foreach (var gamePlanet in game.planets)
            {
                if (gamePlanet.id == game.playerPlanetId)
                {
                    if (Input.GetMouseButtonDown(0) && gamePlanet.currentRocketCooldown <= 0)
                    {
                        var planetPos = gameCamera.WorldToScreenPoint(gamePlanet.currentView.transform.position);
                        Debug.Log($"planet screen pos: {planetPos} mouse pos{Input.mousePosition}");
                        var dir = Input.mousePosition - planetPos;
                        game.Shoot(gamePlanet, dir);
                    }
                }
                else
                {
                    AI.DoSmth(game, gamePlanet);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) paused.value = true;
        }
    }
}