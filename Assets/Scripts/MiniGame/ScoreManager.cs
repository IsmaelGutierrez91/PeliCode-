namespace PeliCode.MiniGame
{
    /// <summary>
    /// Lleva la cuenta de aciertos/preguntas respondidas durante una partida.
    /// Se reinstancia (o resetea) al empezar cada partida en MiniGameManager.
    /// </summary>
    public class ScoreManager
    {
        public int TotalAnswered { get; private set; }
        public int CorrectAnswers { get; private set; }

        public void RegisterAnswer(bool wasCorrect)
        {
            TotalAnswered++;
            if (wasCorrect) CorrectAnswers++;
        }

        public float GetPercentage() =>
            TotalAnswered == 0 ? 0f : (CorrectAnswers / (float)TotalAnswered) * 100f;

        public void Reset()
        {
            TotalAnswered = 0;
            CorrectAnswers = 0;
        }
    }
}
