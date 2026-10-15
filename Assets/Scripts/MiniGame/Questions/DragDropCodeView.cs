using System;
using System.Collections.Generic;
using System.Linq;
using PeliCode.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.MiniGame.Questions
{
    public class DragDropCodeView : MonoBehaviour, IQuestionView
    {
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Transform blocksContainer;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private DragDropBlock blockPrefab;
        [SerializeField] private DragDropSlot slotPrefab;
        [SerializeField] private Button checkButton;

        public event Action<bool> OnAnswered;
        public GameObject GameObject => gameObject;

        private QuestionData _question;
        private readonly List<DragDropSlot> _slots = new List<DragDropSlot>();
        private readonly List<DragDropBlock> _blocks = new List<DragDropBlock>();

        public void Setup(QuestionData question)
        {
            _question = question;
            questionText.text = question.questionText;

            // Se destruyen los bloques de la pregunta anterior esten donde esten
            // (lista, casilla o a mitad de un arrastre), y despues las casillas.
            foreach (var block in _blocks)
                if (block != null) DestroyNow(block.gameObject);
            ClearChildren(blocksContainer);
            ClearChildren(slotsContainer);
            _slots.Clear();
            _blocks.Clear();

            for (int i = 0; i < question.correctOrder.Count; i++)
            {
                var slot = Instantiate(slotPrefab, slotsContainer);
                _slots.Add(slot);
            }

            var shuffledIndices = Enumerable.Range(0, question.codeBlocks.Count)
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            foreach (int blockIndex in shuffledIndices)
            {
                var block = Instantiate(blockPrefab, blocksContainer);
                block.Initialize(blockIndex, question.codeBlocks[blockIndex], blocksContainer);
                _blocks.Add(block);
            }

            checkButton.interactable = true;
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(CheckAnswer);
        }

        private void CheckAnswer()
        {
            var currentOrder = new List<int>();
            foreach (var slot in _slots)
            {
                var block = slot.GetComponentInChildren<DragDropBlock>();
                currentOrder.Add(block != null ? block.BlockIndex : -1);
            }

            bool correct = currentOrder.SequenceEqual(_question.correctOrder);

            Lock();
            OnAnswered?.Invoke(correct);
        }

        private void Lock()
        {
            checkButton.interactable = false;
            foreach (var block in _blocks)
                if (block != null) block.SetDraggable(false);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyNow(parent.GetChild(i).gameObject);
        }

        // Se saca del padre antes de destruir: Destroy es diferido y, mientras tanto,
        // el GridLayoutGroup acomodaria los bloques viejos junto a los nuevos.
        private static void DestroyNow(GameObject go)
        {
            go.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        public void Clear()
        {
            Lock();
            checkButton.onClick.RemoveAllListeners();
        }
    }
}
