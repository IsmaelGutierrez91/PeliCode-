using System;
using System.Collections.Generic;

namespace PeliCode.Data
{
    /// <summary>
    /// Tipos de minijuego soportados. Cada pregunta declara a cual pertenece
    /// para que el MiniGameManager instancie la vista correcta.
    /// </summary>
    public enum QuestionType
    {
        SeleccionMultiple,      // 4 botones de respuesta
        CompletarCodigo,        // input field, el usuario escribe la respuesta
        ArrastrarCodigo,        // arrastrar bloques para ordenar el codigo
        DetectarError           // seleccionar el bloque de codigo con el error
    }

    /// <summary>
    /// Modelo unico de pregunta. No todos los campos se usan en todos los
    /// QuestionType; ver comentarios por campo. Se guarda en Unity Cloud Save
    /// como "Custom Data" (no ligado a un jugador), indexado por categoria+dificultad.
    /// </summary>
    [Serializable]
    public class QuestionData
    {
        public string id;
        public MiniGameCategory category;
        public Difficulty difficulty;
        public QuestionType type;

        public string questionText;

        // --- SeleccionMultiple ---
        // En vez de guardar las 4 opciones fijas, se guarda 1 respuesta correcta y
        // un pool de 8 respuestas incorrectas (todas distintas entre si). En cada
        // partida, MultipleChoiceView arma los 4 botones tomando la correcta + 3
        // incorrectas al azar de ese pool, y baraja el orden de los 4 botones.
        // Esto evita que el jugador memorice la posicion o el set fijo de opciones.
        public string correctAnswer;
        public List<string> incorrectAnswerPool; // minimo 8 respuestas, sin repetidos

        // --- CompletarCodigo ---
        public string codeTemplate;          // codigo con "____" donde el usuario escribe
        public string expectedAnswer;        // respuesta esperada (trim + case-insensitive opcional)

        // --- ArrastrarCodigo ---
        public List<string> codeBlocks;      // bloques desordenados a mostrar
        public List<int> correctOrder;       // orden correcto por indice de codeBlocks

        // --- DetectarError ---
        public List<string> codeSnippetsOptions; // varios bloques de codigo (botones)
        public int buggyIndex;                    // indice del bloque que contiene el error
    }

    /// <summary>
    /// Wrapper para deserializar una coleccion de preguntas desde Cloud Save.
    /// </summary>
    [Serializable]
    public class QuestionSet
    {
        public List<QuestionData> questions = new List<QuestionData>();
    }
}
