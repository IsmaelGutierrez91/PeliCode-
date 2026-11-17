using System;
using PeliCode.Data;
using UnityEngine;

namespace PeliCode.MiniGame.Questions
{
    /// <summary>
    /// Contrato comun para las 4 vistas de minijuego (Seleccion multiple,
    /// Completar codigo, Arrastrar codigo, Detectar error). El MiniGameManager
    /// activa el GameObject correspondiente al QuestionType y llama a Setup().
    /// </summary>
    public interface IQuestionView
    {
        event Action<bool> OnAnswered; // true = correcto, false = incorrecto
        void Setup(QuestionData question);
        void Clear();

        /// <summary>
        /// Despues de un error (o de acabarse el tiempo): muestra la respuesta correcta en la vista.
        /// Devuelve un texto para el mensaje de abajo (por ejemplo, la respuesta esperada), o null.
        /// </summary>
        string RevealAnswer();

        /// <summary>Segundos que se deja ver la respuesta antes de pasar a lo siguiente despues de un error.</summary>
        float MistakeDelaySeconds { get; }

        GameObject GameObject { get; }

        /// <summary>Lo normal: el tiempo de leer "Incorrecto" y la respuesta.</summary>
        public const float DefaultMistakeDelay = 2.5f;
    }
}
