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
        GameObject GameObject { get; }
    }
}
