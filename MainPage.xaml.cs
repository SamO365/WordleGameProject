using System.Net.Http;
using System.IO;
using System;
using System.Text.Json;

namespace WordleGameProject
{
    public partial class MainPage : ContentPage
    {
        private wordleGame currentGame;
        private List<string> wordList;
        public MainPage()
        {
            InitializeComponent();
            LoadGameAsync();
        }



        public static class WordMaker
        {
            private static readonly string website = "https://raw.githubusercontent.com/DonH-ITS/jsonfiles/main/words.txt";
            private static readonly string file = "words.txt";



            //using the httpclient, it will download the list of words then store them in a local directory 
            // then checks if the file exists to read from instead of redownloading everytime
            public static async Task<List<string>> GetWordListAsync()
            {
                string filePath = Path.Combine(FileSystem.AppDataDirectory, file);

                if (!File.Exists(filePath))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var content = await client.GetStringAsync(website);
                        await File.WriteAllTextAsync(filePath, content);
                    }
                }

                string[] words = await File.ReadAllLinesAsync(filePath);
                return words.Where(word => word.Length == 5).ToList();
            }
        }

        //creating the wordle game
        public class wordleGame
        {
            public string chosenWord;
            public int attempts = 6;

            public wordleGame(List<string> words)
            {
                var random = new Random();
                chosenWord = words[random.Next(words.Count)];
            }
            public (string response, bool IsRight) SubmitGuess(string guess)
            {
                // if the guess isnt 5 letters, instead of nuking the program it will write a message saying the following
                //for extra security if there is an empty space it will also return the same
                if (string.IsNullOrEmpty(guess) || guess.Length != 5)
                {
                    throw new Exception("Your Guess has to be 5 letters! Try Again!");
                }
                if (attempts <= 0)
                {
                    return ("Game Over! You have no attempts left", false);
                }
                attempts--;

                var response = new char[5];
                var wordArray = chosenWord.ToCharArray();

                for (int i = 0; i < 5; i++)
                {
                    if (guess[i] == wordArray[i])
                    {
                        response[i] = '1';//when the value is 1 it is correct
                        wordArray[i] = '0';// 0 marks it as used
                    }
                    else if (wordArray.Contains(guess[i]))
                    {
                        response[i] = '!';// ! marks it as wrong
                        wordArray[Array.IndexOf(wordArray, guess[i])] = '0';//marks as wrong
                    }
                    else
                    {
                        response[i] = '*';//if the letter is not part of the word * is the result
                    }
                }
                bool isRight = guess == chosenWord;
                return (new string(response), isRight);

            }
            //created a new class to allow for game to be saved
            public class Progress
            {
                public string playerName { get; set; }
                public List<GameResult> GameHistory { get; set; } = new List<GameResult>();

                public static string GetFilePath(string playerName)
                {
                    return Path.Combine(FileSystem.AppDataDirectory, $"{playerName}.json");
                }

                public static async Task<Progress> LoadAsync(string playerName)
                {
                    string filePath = GetFilePath(playerName);
                    if (File.Exists(filePath))
                    {
                        string json = await File.ReadAllTextAsync(filePath);
                        return JsonSerializer.Deserialize<Progress>(json);
                    }
                    return new Progress { playerName = playerName };
                }

                public async Task SaveAsync()
                {
                    string filePath = GetFilePath(playerName);
                    string json = JsonSerializer.Serialize(this);
                    await File.WriteAllTextAsync(filePath, json);
                }
            }
            public class GameResult
            {
                public string chosenWord { get; set; }
                public int attempts { get; set; }
                public string gameGrid { get; set; }
                public DateTime Timestamp { get; set; }
            }
        }

        private async void LoadGameAsync()
        {
            wordList = await WordMaker.GetWordListAsync();
            StartNewGame();
        }

        private void StartNewGame()
        {
            currentGame = new wordleGame(wordList);
            responseContainer.Children.Clear();
            UpdateAttemptsLabel();
        }

        private void OnSubmitGuessClicked(object sender, EventArgs e)
        {
            string guess = $"{L1.Text}{L2.Text}{L3.Text}{L4.Text}{L5.Text}".ToUpper();

            if (string.IsNullOrEmpty(guess) || guess.Length != 5)
            {
                DisplayAlert("Error", "Please enter a valid 5-letter word.", "OK");
                return;
            }

            var (response, isRight) = currentGame.SubmitGuess(guess);

            responseContainer.Children.Add(new Label
            {
                Text = $"{guess}: {response}",
                FontSize = 18,
                TextColor = isRight ? Colors.Green : Colors.Black
            });

            if (isRight)
            {
                DisplayAlert("Congratulations!", "You guessed the word!", "OK");
                StartNewGame();
            }
            else if (currentGame.attempts == 0)
            {
                DisplayAlert("Game Over", $"The word was: {currentGame.chosenWord}", "OK");
                StartNewGame();
            }

            UpdateAttemptsLabel();

            L1.Text = string.Empty;
            L2.Text = string.Empty;
            L3.Text = string.Empty;
            L4.Text = string.Empty;
            L5.Text = string.Empty;

            L1.Focus(); //sets focus to the first box
        }

        private void UpdateAttemptsLabel()
        {
            AttemptsLabel.Text = $"Attempts Left: {currentGame.attempts}";
        }

        private void OnNewGameClicked(object sender, EventArgs e)
        {
            StartNewGame();
        }
    }
}